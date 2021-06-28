﻿using CaseManagement.Common.Expression;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace CaseManagement.CMMN.Domains
{
    [Serializable]
    public class CasePlanInstanceExecutionContext : ExpressionExecutionContext, ICloneable
    {
        public CasePlanInstanceExecutionContext()
        {
            Variables = new Dictionary<string, string>();
        }

        public CasePlanInstanceExecutionContext(CasePlanInstanceAggregate casePlanInstance) : this()
        {
            CasePlanInstance = casePlanInstance;
        }

        [IgnoreDataMember]
        [JsonIgnore]
        public CasePlanInstanceAggregate CasePlanInstance { get; set; }
        public Dictionary<string, string> Variables;

        public int GetVariableAndIncrement(string key)
        {
            int result = 0;
            if (!Variables.ContainsKey(key))
            {
                SetStrVariable(key, "1");
            }
            else
            {
                result = int.Parse(Variables[key]);
                SetStrVariable(key, (result + 1).ToString());
            }

            return result;
        }

        public string GetStrVariable(string key)
        {
            if (!Variables.ContainsKey(key))
            {
                return null;
            }

            return Variables[key];
        }

        public void SetStrVariable(string key, string value)
        {
            CasePlanInstance.SetVariable(key, value);
        }

        internal bool UpdateStrVariable(string key, string value)
        {
            if (!Variables.ContainsKey(key))
            {
                return Variables.TryAdd(key, value);
            }
            else
            {
                Variables[key] = value;
            }

            return true;
        }

        public object Clone()
        {
            return new CasePlanInstanceExecutionContext
            {
                Variables = Variables.ToDictionary(c => c.Key, c => c.Value)
            };
        }
    }
}