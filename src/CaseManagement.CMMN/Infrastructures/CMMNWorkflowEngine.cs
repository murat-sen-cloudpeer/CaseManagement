﻿using CaseManagement.CMMN.CaseInstance.Exceptions;
using CaseManagement.CMMN.CaseInstance.Processors;
using CaseManagement.CMMN.CaseInstance.Processors.Listeners;
using CaseManagement.CMMN.Domains;
using CaseManagement.CMMN.Domains.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaseManagement.CMMN.Infrastructures
{
    public class CMMNWorkflowEngine : ICMMNWorkflowEngine
    {
        private readonly IEnumerable<IProcessor> _cmmnPlanItemProcessors;

        public CMMNWorkflowEngine(IEnumerable<IProcessor> cmmnPlanItemProcessors)
        {
            _cmmnPlanItemProcessors = cmmnPlanItemProcessors;
        }

        public Task Start(CMMNWorkflowDefinition workflowDefinition, CMMNWorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var cancellationTokenSource = new CancellationTokenSource();
            var task = new Task(() => HandleTask(workflowDefinition, workflowInstance, cancellationTokenSource));
            task.Start();
            return task;
        }

        public Task Reactivate(CMMNWorkflowDefinition workflowDefinition, CMMNWorkflowInstance workflowInstance, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            workflowInstance.MakeTransition(CMMNTransitions.Reactivate);
            var cancellationTokenSource = new CancellationTokenSource();
            var task = new Task(() => HandleTask(workflowDefinition, workflowInstance, cancellationTokenSource, true));
            task.Start();
            return task;
        }

        private void HandleTask(CMMNWorkflowDefinition workflowDefinition, CMMNWorkflowInstance workflowInstance, CancellationTokenSource cancellationTokenSource, bool reactivate = false)
        {
            var createListener = new CreateListener(workflowDefinition, workflowInstance, _cmmnPlanItemProcessors, cancellationTokenSource.Token);
            createListener.Listen();
            var repetitionListener = new RepetitionListener(workflowDefinition, workflowInstance);
            repetitionListener.Listen();
            foreach (var element in workflowDefinition.Elements)
            {
                if (!reactivate)
                {
                    workflowInstance.CreateWorkflowElementInstance(element);
                }
                else
                {
                    foreach (var elt in workflowInstance.WorkflowElementInstances.Where(w => w.State == Enum.GetName(typeof(CMMNTaskStates), CMMNTaskStates.Active)).ToList())
                    {
                        var parameter = new ProcessorParameter(workflowDefinition, workflowInstance, workflowInstance.GetWorkflowElementInstance(elt.Id));
                        var processor = _cmmnPlanItemProcessors.First(p => p.Type == elt.WorkflowElementDefinitionType);
                        processor.Handle(parameter, cancellationTokenSource.Token).ContinueWith((obj) =>
                        {
                            var result = obj.Result;
                            if (result.WorkflowInstance.IsRepetitionRuleSatisfied(result.WorkflowElementInstance.WorkflowElementDefinitionId, result.WorkflowDefinition, false))
                            {
                                result.WorkflowInstance.CreateWorkflowElementInstance(result.WorkflowElementInstance.WorkflowElementDefinitionId, result.WorkflowElementInstance.WorkflowElementDefinitionType);
                                return;
                            }

                            result.WorkflowInstance.FinishElement(result.WorkflowElementInstance.WorkflowElementDefinitionId);
                        });
                    }
                }
            }

            var children = workflowDefinition.Elements.Select(e => e.Id);
            bool continueExecution = true;
            bool isSuspend = false;
            var reactivateListener = CMMNCaseTransitionListener.Listen(workflowInstance, CMMNTransitions.Reactivate, () =>
            {
                if (isSuspend)
                {
                    isSuspend = false;
                    var workflowElementInstances = workflowInstance.WorkflowElementInstances;
                    foreach (var workflowElementInstance in workflowElementInstances)
                    {
                        if (workflowElementInstance.State == Enum.GetName(typeof(CMMNTaskStates), CMMNTaskStates.Suspended))
                        {
                            workflowInstance.MakeTransition(workflowElementInstance.Id, CMMNTransitions.ParentResume);
                        }
                    }
                }
            });
            var suspendListener = CMMNCaseTransitionListener.Listen(workflowInstance, CMMNTransitions.Suspend, () =>
            {
                var workflowElementInstances = workflowInstance.WorkflowElementInstances;
                foreach (var workflowElementInstance in workflowElementInstances)
                {
                    if (workflowElementInstance.State == Enum.GetName(typeof(CMMNTaskStates), CMMNTaskStates.Active))
                    {
                        workflowInstance.MakeTransition(workflowElementInstance.Id, CMMNTransitions.ParentSuspend);
                    }
                }

                isSuspend = true;
            });
            var terminateListener = CMMNCaseTransitionListener.Listen(workflowInstance, CMMNTransitions.Terminate, () =>
            {
                var workflowElementInstances = workflowInstance.WorkflowElementInstances;
                foreach (var workflowElementInstance in workflowElementInstances)
                {
                    if (workflowElementInstance.State == Enum.GetName(typeof(CMMNTaskStates), CMMNTaskStates.Active))
                    {
                        workflowInstance.MakeTransition(workflowElementInstance.Id, CMMNTransitions.ParentTerminate);
                    }
                }

                continueExecution = false;
            });
            var kvp = CMMNCriterionListener.ListenExitCriterias(new ProcessorParameter(null, workflowInstance, new CMMNWorkflowElementInstance(null, DateTime.UtcNow, null, CMMNWorkflowElementTypes.Stage, 0, null)), workflowDefinition.ExitCriterias);
            if (kvp != null)
            {
                try
                {
                    kvp.Value.Key.ContinueWith((r) =>
                    {
                        r.Wait();
                        workflowInstance.MakeTransition(CMMNTransitions.Terminate);
                    });
                }
                catch (TerminateCaseInstanceElementException)
                {
                    workflowInstance.MakeTransition(CMMNTransitions.Terminate);
                }
            }

            while (continueExecution)
            {
                Thread.Sleep(100);
                if (isSuspend)
                {
                    continue;
                }

                try
                {
                    cancellationTokenSource.Token.ThrowIfCancellationRequested();
                    if (children.All(c => workflowInstance.IsWorkflowElementDefinitionFinished(c)))
                    {
                        continueExecution = false;
                        workflowInstance.MakeTransition(CMMNTransitions.Complete);
                    }

                    if (children.Any(c => workflowInstance.IsWorkflowElementDefinitionFailed(c) && workflowDefinition.GetElement(c).Type != CMMNWorkflowElementTypes.Stage))
                    {
                        continueExecution = false;
                        workflowInstance.MakeTransition(CMMNTransitions.Fault);
                    }
                }
                catch (OperationCanceledException)
                {
                    continueExecution = false;
                }
            }

            reactivateListener.Unsubscribe();
            suspendListener.Unsubscribe();
            terminateListener.Unsubscribe();
            createListener.Unsubscribe();
            repetitionListener.Unsubscribe();
            cancellationTokenSource.Cancel();
            if (kvp != null)
            {
                if (kvp.Value.Key.IsCanceled || kvp.Value.Key.IsCompleted || kvp.Value.Key.IsFaulted)
                {
                    kvp.Value.Key.Dispose();
                }

                kvp.Value.Value.Unsubscribe();
            }
        }

        private class CreateListener
        {
            private readonly CMMNWorkflowDefinition _workflowDefinition;
            private readonly CMMNWorkflowInstance _workflowInstance;
            private readonly IEnumerable<IProcessor> _processors;
            private readonly CancellationToken _token;

            public CreateListener(CMMNWorkflowDefinition workflowDefinition, CMMNWorkflowInstance workflowInstance, IEnumerable<IProcessor> processors, CancellationToken token)
            {
                _workflowDefinition = workflowDefinition;
                _workflowInstance = workflowInstance;
                _processors = processors;
                _token = token;
            }

            public void Listen()
            {
                _workflowInstance.EventRaised += HandleEventRaised;
            }

            public void Unsubscribe()
            {
                _workflowInstance.EventRaised -= HandleEventRaised;
            }

            private void HandleEventRaised(object sender, DomainEventArgs args)
            {
                var elementCreated = args.DomainEvt as CMMNWorkflowElementCreatedEvent;
                if (elementCreated == null)
                {
                    return;
                }

                var processor = _processors.First(p => p.Type == elementCreated.WorkflowElementDefinitionType);
                var parameter = new ProcessorParameter(_workflowDefinition, _workflowInstance, _workflowInstance.GetWorkflowElementInstance(elementCreated.ElementId));
                var workflowElementInstance = parameter.WorkflowInstance.GetLastWorkflowElementInstance(elementCreated.WorkflowElementDefinitionId);
                if (workflowElementInstance == null || workflowElementInstance.Version == 0)
                {
                    _workflowInstance.StartElement(elementCreated.WorkflowElementDefinitionId);
                }

                // Note : Ignore TimerEventListener and CaseFileItem.
                if (workflowElementInstance.Version > 0 && (workflowElementInstance.WorkflowElementDefinitionType == CMMNWorkflowElementTypes.TimerEventListener || workflowElementInstance.WorkflowElementDefinitionType == CMMNWorkflowElementTypes.CaseFileItem))
                {
                    return;
                }

                processor.Handle(parameter, _token).ContinueWith((obj) =>
                {
                    var result = obj.Result;
                    if (result.WorkflowInstance.IsRepetitionRuleSatisfied(result.WorkflowElementInstance.WorkflowElementDefinitionId, result.WorkflowDefinition, false))
                    {
                        result.WorkflowInstance.CreateWorkflowElementInstance(result.WorkflowElementInstance.WorkflowElementDefinitionId, result.WorkflowElementInstance.WorkflowElementDefinitionType);
                        return;
                    }

                    result.WorkflowInstance.FinishElement(result.WorkflowElementInstance.WorkflowElementDefinitionId);
                });
            }
        }

        private class RepetitionListener
        {
            private readonly CMMNWorkflowDefinition _workflowDefinition;
            private readonly CMMNWorkflowInstance _workflowInstance;

            public RepetitionListener(CMMNWorkflowDefinition workflowDefinition, CMMNWorkflowInstance workflowInstance)
            {
                _workflowDefinition = workflowDefinition;
                _workflowInstance = workflowInstance;
            }

            public void Listen()
            {
                _workflowInstance.EventRaised += HandleEventRaised;
            }

            public void Unsubscribe()
            {
                _workflowInstance.EventRaised -= HandleEventRaised;
            }

            private void HandleEventRaised(object sender, DomainEventArgs e)
            {
                var raisedEvt = e.DomainEvt as CMMNWorkflowElementTransitionRaisedEvent;
                if (raisedEvt == null)
                {
                    return;
                }

                var sourcePlanItemInstance = _workflowInstance.GetWorkflowElementInstance(raisedEvt.ElementId);
                foreach (var planItem in _workflowDefinition.Elements)
                {
                    if(planItem.EntryCriterions.Any(ec => ec.SEntry.PlanItemOnParts.Any(pi => pi.StandardEvent == raisedEvt.Transition && pi.SourceRef == sourcePlanItemInstance.WorkflowElementDefinitionId)) && planItem.ActivationRule == CMMNActivationRuleTypes.Repetition)
                    {
                        if (_workflowInstance.IsRepetitionRuleSatisfied(planItem.Id, _workflowDefinition, true))
                        {
                            _workflowInstance.CreateWorkflowElementInstance(planItem.Id, planItem.Type);
                        }
                        else
                        {
                            _workflowInstance.FinishElement(planItem.Id);
                        }
                    }
                }
            }
        }
    }
}