﻿using CaseManagement.CMMN.Domains.CaseFile;
using CaseManagement.CMMN.Persistence.Parameters;
using CaseManagement.CMMN.Persistence.Responses;
using System.Threading.Tasks;

namespace CaseManagement.CMMN.Persistence
{
    public interface ICMMNWorkflowFileQueryRepository
    {
        Task<CaseFileDefinitionAggregate> FindById(string id);
        Task<FindResponse<CaseFileDefinitionAggregate>> Find(FindCaseDefinitionFilesParameter parameter);
    }
}