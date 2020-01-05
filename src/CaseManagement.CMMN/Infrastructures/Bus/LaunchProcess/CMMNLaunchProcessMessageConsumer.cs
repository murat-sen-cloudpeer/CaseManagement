﻿using CaseManagement.CMMN.Domains;
using CaseManagement.CMMN.Persistence;
using CaseManagement.Workflow.Infrastructure;
using CaseManagement.Workflow.Infrastructure.Bus;
using CaseManagement.Workflow.Infrastructure.EvtStore;
using CaseManagement.Workflow.Infrastructure.Lock;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CaseManagement.CMMN.Infrastructures.Bus.LaunchProcess
{
    public class CMMNLaunchProcessMessageConsumer : BaseMessageConsumer
    {
        private readonly ILogger _logger;
        private readonly IDistributedLock _distributedLock;
        private readonly ICMMNWorkflowEngine _workflowEngine;
        private readonly ICommitAggregateHelper _commitAggregateHelper;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly ICMMNWorkflowDefinitionQueryRepository _cmmnWorkflowDefinitionQueryRepository;

        public CMMNLaunchProcessMessageConsumer(ILogger<ReactivateProcessMessageConsumer> logger, IDistributedLock distributedLock, ICMMNWorkflowEngine workflowEngine, ICommitAggregateHelper commitAggregateHelper, IEventStoreRepository eventStoreRepository, ICMMNWorkflowDefinitionQueryRepository cmmnWorkflowDefinitionQueryRepository, IRunningTaskPool taskPool, IQueueProvider queueProvider, IOptions<BusOptions> options) : base(taskPool, queueProvider, options)
        {
            _logger = logger;
            _distributedLock = distributedLock;
            _workflowEngine = workflowEngine;
            _commitAggregateHelper = commitAggregateHelper;
            _eventStoreRepository = eventStoreRepository;
            _cmmnWorkflowDefinitionQueryRepository = cmmnWorkflowDefinitionQueryRepository;
        }

        public override string QueueName => QUEUE_NAME;
        public const string QUEUE_NAME = "launch-process";

        protected override async Task<RunningTask> Execute(string queueMessage)
        {
            var message = JsonConvert.DeserializeObject<LaunchProcessMessage>(queueMessage);
            var cancellationTokenSource = new CancellationTokenSource();
            var lockId = message.ProcessFlowId;
            await QueueProvider.Dequeue(QueueName);
            if (!await _distributedLock.AcquireLock(lockId))
            {
                _logger.LogDebug($"The process flow {lockId} is locked !");
                return null;
            }

            var workflowInstance = await _eventStoreRepository.GetLastAggregate<CMMNWorkflowInstance>(message.ProcessFlowId, CMMNWorkflowInstance.GetStreamName(message.ProcessFlowId));
            var workflowDefinition = await _cmmnWorkflowDefinitionQueryRepository.FindById(workflowInstance.WorkflowDefinitionId);
            var task = new Task(async () => await HandleLaunchProcess(workflowDefinition, workflowInstance, message.ProcessFlowId, cancellationTokenSource.Token));
            return new RunningTask(message.ProcessFlowId, task, workflowInstance, cancellationTokenSource);
        }

        private async Task HandleLaunchProcess(CMMNWorkflowDefinition workflowDefinition, CMMNWorkflowInstance workflowInstance, string taskId, CancellationToken token)
        {
            var lockId = workflowInstance.Id;
            Debug.WriteLine($"Launch process {lockId}");
            try
            {
                try
                {
                    workflowInstance.EventRaised += HandleEventRaised;
                    await _workflowEngine.Start(workflowDefinition, workflowInstance, token);
                    token.ThrowIfCancellationRequested();
                }
                finally
                {
                    workflowInstance.EventRaised -= HandleEventRaised;
                }
            }
            finally
            {
                TaskPool.RemoveTask(taskId);
                await _distributedLock.ReleaseLock(lockId);
            }
        }

        private async void HandleEventRaised(object sender, DomainEventArgs e)
        {
            var workflowInstance = sender as CMMNWorkflowInstance;
            await _commitAggregateHelper.Commit(workflowInstance, new List<DomainEvent> { e.DomainEvt }, workflowInstance.Version, workflowInstance.GetStreamName());
        }
    }
}