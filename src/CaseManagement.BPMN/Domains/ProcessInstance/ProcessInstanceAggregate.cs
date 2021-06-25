﻿using CaseManagement.BPMN.Helpers;
using CaseManagement.Common.Domains;
using DynamicExpresso;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CaseManagement.BPMN.Domains
{
    public class ProcessInstanceAggregate : BaseAggregate
    {
        public ProcessInstanceAggregate()
        {
            SerializedElementDefs = new List<SerializedFlowNodeDefinition>();
            ElementInstances = new List<FlowNodeInstance>();
            ExecutionPathLst = new List<ExecutionPath>();
            ItemDefs = new List<ItemDefinition>();
            SequenceFlows = new List<SequenceFlow>();
            Interfaces = new List<BPMNInterface>();
            Messages = new List<Message>();
            StateTransitions = new List<StateTransitionToken>();
        }

        public ProcessInstanceStatus Status { get; set; }
        public string ProcessFileId { get; set; }
        public string NameIdentifier { get; set; }
        public string ProcessFileName { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public ICollection<StartEvent> StartEvts => ElementDefs.Where(_ => _ is StartEvent).Cast<StartEvent>().ToList();
        public ICollection<BaseFlowNode> ElementDefs
        {
            get
            {
                return SerializedElementDefs.Select(e => e.Deserialize()).ToList();
            }
        }
        public ICollection<ItemDefinition> ItemDefs { get; set; }
        public ICollection<BPMNInterface> Interfaces { get; set; }
        public ICollection<Message> Messages { get; set; }
        public ICollection<SerializedFlowNodeDefinition> SerializedElementDefs { get; set; }
        public ICollection<SequenceFlow> SequenceFlows { get; set; }
        public ICollection<FlowNodeInstance> ElementInstances { get; set; }
        public ICollection<ExecutionPath> ExecutionPathLst { get; set; }
        public ICollection<StateTransitionToken> StateTransitions { get; set; }

        #region Getters

        public bool IsIncomingSatisfied(SequenceFlow incomingSequence, IEnumerable<MessageToken> incomingTokens)
        {
            if (!string.IsNullOrWhiteSpace(incomingSequence.ConditionExpression))
            {
                return EvaluateConditionalExpression(incomingTokens, incomingSequence.ConditionExpression);
            }

            return true;
        }

        public BaseFlowNode GetDefinition(string elementId)
        {
            return ElementDefs.FirstOrDefault(_ => _.EltId == elementId);
        }

        public BaseActivity GetActivity(string elementId)
        {
            return GetDefinition(elementId) as BaseActivity;
        }

        public FlowNodeInstance GetInstance(string id)
        {
            return ElementInstances.FirstOrDefault(_ => _.EltId == id);
        }

        public FlowNodeInstance GetActiveInstance(string elementId)
        {
            return ElementInstances.FirstOrDefault(_ => _.EltId == elementId && _.State == FlowNodeStates.Active);
        }

        public ExecutionPath GetExecutionPath(string executionPathId)
        {
            return ExecutionPathLst.FirstOrDefault(_ => _.Id == executionPathId);
        }

        public ExecutionPointer GetExecutionPointer(string executionPathId, string executionPointerId)
        {
            var path = GetExecutionPath(executionPathId);
            return path.Pointers.FirstOrDefault(_ => _.Id == executionPointerId);
        }

        public ExecutionPointer GetActiveExecutionPointer(string executionPathId, string flowInstanceId)
        {
            var path = GetExecutionPath(executionPathId);
            return path.Pointers.FirstOrDefault(_ => _.InstanceFlowNodeId == flowInstanceId && _.IsActive);
        }

        public Message GetMessage(string messageRef)
        {
            return Messages.FirstOrDefault(_ => _.EltId == messageRef);
        }

        public Operation GetOperation(string operationRef)
        {
            return Interfaces.SelectMany(_ => _.Operations).FirstOrDefault(_ => _.EltId == operationRef);
        }

        public ItemDefinition GetItem(string itemRef)
        {
            return ItemDefs.FirstOrDefault(_ => _.EltId == itemRef);
        }

        public bool IsMessageCorrect(MessageToken messageToken)
        {
            var message = GetMessage(messageToken.Name);
            if (message == null || messageToken.Name != messageToken.Name)
            {
                return false;
            }

            var item = GetItem(message.ItemRef);
            if (item == null)
            {
                return true;
            }

            var type = TypeResolver.ResolveType(item.StructureRef);
            if (item ==  null || type == null)
            {
                return false;
            }

            object result = null;
            try
            {
                result = JsonConvert.DeserializeObject(messageToken.MessageContent.ToString(), type);
            }
            catch
            {
                return false;
            }

            return result != null;
        }

        public ICollection<SequenceFlow> GetIncomingSequenceFlows(string elementId)
        {
            return SequenceFlows.Where(_ => _.TargetRef == elementId).ToList();
        }

        public ICollection<SequenceFlow> GetOutgoingSequenceFlows(string elementId)
        {
            return SequenceFlows.Where(_ => _.SourceRef == elementId).ToList();
        }

        #endregion

        #region Operations

        public void Start(string nameIdentifier)
        {
            var evt = new ProcessInstanceStartedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, nameIdentifier, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
        }

        public void NewExecutionPath()
        {
            var pathId = Guid.NewGuid().ToString();
            var evt = new ExecutionPathCreatedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, pathId, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
            foreach (var startEvt in StartEvts)
            {
                TryAddExecutionPointer(pathId, startEvt);
            }
        }

        public ICollection<string> CompleteExecutionPointer(ExecutionPointer pointer, ICollection<string> nextFlowIds, ICollection<MessageToken> outcomeValues)
        {
            var evt = new ExecutionPointerCompletedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, pointer.ExecutionPathId, pointer.Id, outcomeValues, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
            var result = new List<string>();
            foreach (var nextFlowId in nextFlowIds)
            {
                result.Add(TryAddExecutionPointer(pointer.ExecutionPathId, GetDefinition(nextFlowId), outcomeValues));
            }

            return result;
        }

        public string LaunchNewExecutionPointer(ExecutionPointer pointer)
        {
            return TryAddExecutionPointer(pointer.ExecutionPathId, GetDefinition(pointer.FlowNodeId));
        }

        public void CompleteFlowNodeInstance(string id)
        {
            var evt = new FlowNodeInstanceCompletedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, id, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
        }

        public void UpdateState(FlowNodeInstance instance, ActivityStates state, string message = null)
        {
            var evt = new ActivityStateUpdatedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, instance.EltId, state, message, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
        }

        public void AddFlowNodeDef(BaseFlowNode node)
        {
            var evt = new FlowNodeDefCreatedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, JsonConvert.SerializeObject(node),  DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
        }

        public void UpdateMetadata(string flowNodeInstanceId, string key, string value)
        {
            var evt = new MetadataUpdatedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, flowNodeInstanceId, key, value, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
        }

        public void ConsumeStateTransition(string flowNodeInstanceId, string stateTransition, string content)
        {
            var nodeInstance = GetInstance(flowNodeInstanceId);
            if (nodeInstance == null)
            {
                return;
            }

            var evt = new StateTransitionReceivedEvent(Guid.NewGuid().ToString(),
                AggregateId,
                Version + 1,
                flowNodeInstanceId,
                stateTransition,
                content,
                DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
        }

        public void ConsumeMessage(MessageToken messageToken)
        {
            foreach(var executionPath in ExecutionPathLst)
            {
                ConsumeMessage(executionPath, messageToken);
            }
        }

        #endregion

        #region Helpers

        private string TryAddExecutionPointer(string pathId, BaseFlowNode flowNode, ICollection<MessageToken> outcomeValues = null)
        {
            ICollection<MessageToken> incoming = new List<MessageToken>();
            if (outcomeValues != null)
            {
                incoming = outcomeValues;
            }

            var instanceId = string.Empty;
            if (!TryAddInstance(flowNode, pathId, out instanceId))
            {
                var pointer = GetActiveExecutionPointer(pathId, instanceId);
                if (pointer != null)
                {
                    var e = new IncomingTokenAddedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, pointer.ExecutionPathId, pointer.Id, JsonConvert.SerializeObject(incoming), DateTime.UtcNow);
                    Handle(e);
                    DomainEvents.Add(e);
                    return pointer.Id;
                }
            }

            var id = Guid.NewGuid().ToString();
            var evt = new ExecutionPointerAddedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, id, pathId, instanceId, flowNode.EltId, JsonConvert.SerializeObject(incoming), DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
            return id;
        }

        private bool TryAddInstance(BaseFlowNode node, string pathId, out string instanceId)
        {
            return TryAddInstance(node.EltId, pathId, out instanceId);
        }

        private bool TryAddInstance(string elementId, string pathId, out string instanceId)
        {
            var path = GetExecutionPath(pathId);
            var pointer = path.Pointers.FirstOrDefault(_ => _.FlowNodeId == elementId);
            if (pointer != null)
            {
                var instance = GetActiveInstance(pointer.InstanceFlowNodeId);
                if (instance != null)
                {
                    instanceId = instance.EltId;
                    return false;
                }
            }

            instanceId = Guid.NewGuid().ToString();
            var evt = new FlowNodeInstanceAddedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, instanceId, elementId, DateTime.UtcNow);
            Handle(evt);
            DomainEvents.Add(evt);
            return true;
        }

        private void ConsumeMessage(ExecutionPath executionPath, MessageToken messageToken)
        {
            foreach(var pointer in executionPath.ActivePointers)
            {
                var nodeDef = GetDefinition(pointer.FlowNodeId) as BaseCatchEvent;
                if(nodeDef == null)
                {
                    continue;
                }

                if (nodeDef.EventDefinitions.Any(_ => _.IsSatisfied(this, messageToken)))
                {
                    var tokens = new List<MessageToken> { messageToken };
                    var evt = new IncomingTokenAddedEvent(Guid.NewGuid().ToString(), AggregateId, Version + 1, executionPath.Id, pointer.Id, JsonConvert.SerializeObject(tokens), DateTime.UtcNow);
                    Handle(evt);
                    DomainEvents.Add(evt);
                }
            }
        }

        private bool EvaluateConditionalExpression(IEnumerable<MessageToken> incomingTokens, string expression)
        {
            var decodedExpressionBody = HttpUtility.HtmlDecode(expression);
            var context = new ConditionalExpressionContext(incomingTokens);
            var interpreter = new Interpreter().SetVariable("context", context);
            var parsedExpression = interpreter.Parse(decodedExpressionBody);
            return (bool)parsedExpression.Invoke();
        }

        #endregion

        public static ProcessInstanceAggregate New(List<DomainEvent> evts)
        {
            var result = new ProcessInstanceAggregate();
            foreach(var evt in evts)
            {
                result.Handle(evt);
            }

            return result;
        }

        public static ProcessInstanceAggregate New(string processFileId, string processFileName, ICollection<BaseFlowNode> elements, ICollection<BPMNInterface> interfaces, ICollection<Message> messages, ICollection<ItemDefinition> itemDefs, ICollection<SequenceFlow> sequenceFlows)
        {
            var result = new ProcessInstanceAggregate();
            var evt = new ProcessInstanceCreatedEvent(Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), 0, processFileId, processFileName, interfaces, messages, itemDefs, sequenceFlows, DateTime.UtcNow);
            result.Handle(evt);
            result.DomainEvents.Add(evt);
            foreach (var elt in elements)
            {
                result.AddFlowNodeDef(elt);
            }

            return result;
        }

        public override object Clone()
        {
            return new ProcessInstanceAggregate
            {
                AggregateId = AggregateId,
                ProcessFileId = ProcessFileId,
                ProcessFileName = ProcessFileName,
                Version = Version,
                CreateDateTime = CreateDateTime,
                UpdateDateTime = UpdateDateTime,
                Status = Status,
                SequenceFlows = SequenceFlows.Select(_ => (SequenceFlow)_.Clone()).ToList(),
                SerializedElementDefs = SerializedElementDefs.Select(_ => (SerializedFlowNodeDefinition)_.Clone()).ToList(),
                ElementInstances = ElementInstances.Select(_ => (FlowNodeInstance)_.Clone()).ToList(),
                ExecutionPathLst = ExecutionPathLst.Select(_ => (ExecutionPath)_.Clone()).ToList(),
                ItemDefs = ItemDefs.Select(_ => (ItemDefinition)_.Clone()).ToList(),
                Messages = Messages.Select(_ => (Message)_.Clone()).ToList(),
                Interfaces = Interfaces.Select(_ => (BPMNInterface)_.Clone()).ToList(),
                StateTransitions = StateTransitions.Select(_ => (StateTransitionToken)_.Clone()).ToList()
            };
        }

        public string GetStreamName()
        {
            return GetStreamName(AggregateId);
        }

        #region Handle events

        public override void Handle(dynamic evt)
        {
            Handle(evt);
        }

        private void Handle(ProcessInstanceStartedEvent evt)
        {
            Status = ProcessInstanceStatus.STARTED;
            NameIdentifier = evt.NameIdentifier;
            UpdateDateTime = evt.UpdateDateTime;
            Version = evt.Version;
        }

        private void Handle(MetadataUpdatedEvent evt)
        {
            var instance = GetInstance(evt.FlowNodeInstanceId);
            instance.Metadata.Add(evt.Key, evt.Value);
        }

        private void Handle(StateTransitionReceivedEvent evt)
        {
            StateTransitions.Add(StateTransitionToken.Create(evt.FlowNodeInstanceId, evt.State, evt.Content));
            UpdateDateTime = evt.UpdateDateTime;
            Version = evt.Version;
        }

        private void Handle(ProcessInstanceCreatedEvent evt)
        {
            AggregateId = evt.AggregateId;
            Version = evt.Version;
            ProcessFileId = evt.ProcessFileId;
            ProcessFileName = evt.ProcessFileName;
            CreateDateTime = evt.CreateDateTime;
            Status = ProcessInstanceStatus.CREATED;
            Interfaces = evt.Interfaces;
            Messages = evt.Messages;
            ItemDefs = evt.ItemDefs;
            SequenceFlows = evt.SequenceFlows;
        }

        private void Handle(FlowNodeDefCreatedEvent evt)
        {
            var elt = SerializedFlowNodeDefinition.Create(evt.SerializedContent);
            SerializedElementDefs.Add(elt);
            Version = evt.Version;
            UpdateDateTime = evt.UpdateDateTime;
        }

        private void Handle(FlowNodeInstanceAddedEvent evt)
        {
            var instance = new FlowNodeInstance { FlowNodeId = evt.FlowNodeId, EltId = evt.FlowNodeInstanceId };
            ElementInstances.Add(instance);
            Version = evt.Version;
            UpdateDateTime = evt.CreateDateTime;
        }

        private void Handle(ExecutionPathCreatedEvent evt)
        {
            var path = new ExecutionPath { Id = evt.ExecutionPathId, CreateDateTime = evt.CreateDateTime };
            ExecutionPathLst.Add(path);
            Version = evt.Version;
            UpdateDateTime = evt.CreateDateTime;
        }

        private void Handle(ExecutionPointerAddedEvent evt)
        {
            var tokens = MessageToken.DeserializeLst(evt.SerializedTokens);
            var path = GetExecutionPath(evt.ExecutionPathId);
            var executionPoitner = new ExecutionPointer
            {
                Id = evt.ExecutionPointerId,
                ExecutionPathId = path.Id,
                FlowNodeId = evt.FlowNodeId,
                InstanceFlowNodeId = evt.FlowNodeInstanceId,
            };
            executionPoitner.AddIncoming(tokens);
            path.Pointers.Add(executionPoitner);
            Version = evt.Version;
            UpdateDateTime = evt.CreateDateTime;
        }

        private void Handle(ExecutionPointerCompletedEvent evt)
        {
            var pointer = GetExecutionPointer(evt.ExecutionPathId, evt.ExecutionPointerId);
            pointer.IsActive = false;
            pointer.AddOutgoing(evt.OutcomeValues);
            Version = evt.Version;
            UpdateDateTime = evt.CompletionDateTime;
        }

        private void Handle(ActivityStateUpdatedEvent evt)
        {
            var instance = GetInstance(evt.FlowNodeInstanceId);
            instance.UpdateState(evt.State, evt.UpdateDateTime, evt.Message);
            Version = evt.Version;
            UpdateDateTime = evt.UpdateDateTime;
        }

        private void Handle(IncomingTokenAddedEvent evt)
        {
            var pointer = GetExecutionPointer(evt.ExecutionPathId, evt.ExecutionPointerId);
            var tokens = MessageToken.DeserializeLst(evt.SerializedToken);
            pointer.AddIncoming(tokens);
            Version = evt.Version;
            UpdateDateTime = evt.CreateDateTime;
        }

        private void Handle(FlowNodeInstanceCompletedEvent evt)
        {
            var instance = GetInstance(evt.FlowNodeInstanceId);
            instance.State = FlowNodeStates.Complete;
            Version = evt.Version;
            UpdateDateTime = evt.ExecutionDateTime;
        }

        #endregion

        public static string GetStreamName(string id)
        {
            return $"inst{id}";
        }
    }
}