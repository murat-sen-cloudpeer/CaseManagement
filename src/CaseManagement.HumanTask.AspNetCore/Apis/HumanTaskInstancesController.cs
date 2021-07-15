﻿using CaseManagement.Common.Exceptions;
using CaseManagement.HumanTask.Exceptions;
using CaseManagement.HumanTask.HumanTaskInstance.Commands;
using CaseManagement.HumanTask.HumanTaskInstance.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace CaseManagement.HumanTask.AspNetCore.Apis
{
    [Route(HumanTaskConstants.RouteNames.HumanTaskInstances)]
    public class HumanTaskInstancesController : Controller
    {
        private readonly IMediator _mediator;

        public HumanTaskInstancesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        #region Actions

        [HttpPost]
        [Authorize(HumanTaskConstants.ScopeNames.ManageHumanTaskInstance)]
        public Task<IActionResult> Add([FromBody] CreateHumanTaskInstanceCommand parameter, CancellationToken token)
        {
            parameter.IgnorePermissions = true;
            return SharedAdd(parameter, token);
        }

        [HttpPost("me")]
        [Authorize("Authenticated")]
        public Task<IActionResult> AddMe([FromBody] CreateHumanTaskInstanceCommand parameter, CancellationToken token)
        {
            parameter.Claims = User.GetClaims();
            parameter.NameIdentifier = parameter.Claims.GetUserNameIdentifier();
            return SharedAdd(parameter, token);
        }

        [HttpPost("{id}/nominate")]
        [Authorize("Authenticated")]
        public async Task<IActionResult> Nominate(string id, [FromBody] NominateHumanTaskInstanceCommand parameter, CancellationToken token)
        {
            try
            {
                parameter.HumanTaskInstanceId = id;
                parameter.Claims = User.GetClaims();
                await _mediator.Send(parameter, token);
                return new NoContentResult();
            }
            catch(BadRequestException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.BadRequest, Request);
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
            catch (NotAuthorizedException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.Unauthorized, Request);
            }
            catch (BadOperationExceptions ex)
            {
                var valErrors = ex.ValidationErrors.Select(_ => new KeyValuePair<string, string>("bad_request", _)).ToList();
                return this.ToError(valErrors, HttpStatusCode.BadRequest, Request);
            }
        }

        [HttpGet("{id}/claim")]
        [Authorize("Authenticated")]
        public async Task<IActionResult> Claim(string id, CancellationToken token)
        {
            try
            {
                var cmd = new ClaimHumanTaskInstanceCommand { HumanTaskInstanceId = id, Claims = User.GetClaims() };
                await _mediator.Send(cmd, token);
                return new NoContentResult();
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
            catch (NotAuthorizedException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.Unauthorized, Request);
            }
            catch (BadOperationExceptions ex)
            {
                var valErrors = ex.ValidationErrors.Select(_ => new KeyValuePair<string, string>("bad_request", _)).ToList();
                return this.ToError(valErrors, HttpStatusCode.BadRequest, Request);
            }
        }

        [HttpPost("{id}/force/claim")]
        [Authorize(HumanTaskConstants.ScopeNames.ManageHumanTaskInstance)]
        public async Task<IActionResult> Claim(string id, [FromBody] ForceClaimHumanTaskInstanceCommand cmd, CancellationToken token)
        {
            try
            {
                cmd.HumanTaskInstanceId = id;
                await _mediator.Send(cmd, token);
                return new NoContentResult();
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
        }

        [HttpGet("{id}/start")]
        [Authorize("Authenticated")]
        public async Task<IActionResult> Start(string id, CancellationToken token)
        {
            try
            {
                var cmd = new StartHumanTaskInstanceCommand { HumanTaskInstanceId = id, Claims = User.GetClaims() };
                await _mediator.Send(cmd, token);
                return new NoContentResult();
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
            catch (NotAuthorizedException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.Unauthorized, Request);
            }
            catch (BadOperationExceptions ex)
            {
                var valErrors = ex.ValidationErrors.Select(_ => new KeyValuePair<string, string>("bad_request", _)).ToList();
                return this.ToError(valErrors, HttpStatusCode.BadRequest, Request);
            }
        }


        [HttpPost("{id}/force/start")]
        [Authorize(HumanTaskConstants.ScopeNames.ManageHumanTaskInstance)]
        public async Task<IActionResult> Start(string id, [FromBody] ForceStartHumanTaskInstanceCommand cmd, CancellationToken token)
        {
            try
            {
                cmd.HumanTaskInstanceId = id;
                await _mediator.Send(cmd, token);
                return new NoContentResult();
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
        }

        [HttpPost("{id}/complete")]
        [Authorize("Authenticated")]
        public async Task<IActionResult> Complete(string id, [FromBody] CompleteHumanTaskInstanceCommand parameter, CancellationToken token)
        {
            parameter.HumanTaskInstanceId = id;
            parameter.Claims = User.GetClaims();
            try
            {
                await _mediator.Send(parameter, token);
                return new NoContentResult();
            }
            catch (BadRequestException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.BadRequest, Request);
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
            catch (BadOperationExceptions ex)
            {
                var valErrors = ex.ValidationErrors.Select(_ => new KeyValuePair<string, string>("bad_request", _)).ToList();
                return this.ToError(valErrors, HttpStatusCode.BadRequest, Request);
            }
            catch (NotAuthorizedException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.Unauthorized, Request);
            }
        }

        [HttpPost("start")]
        [Authorize("Authenticated")]
        public async Task<IActionResult> Start([FromBody] StartHumanTaskInstanceCommand parameter, CancellationToken token)
        {
            await _mediator.Send(parameter, token);
            return new NoContentResult();
        }

        #endregion

        #region Getters

        [HttpGet("{id}/details")]
        public async Task<IActionResult> GetDetails(string id, CancellationToken token)
        { 
            try
            {
                var result = await _mediator.Send(new GetHumanTaskInstanceDetailsQuery { HumanTaskInstanceId = id }, token);
                return new OkObjectResult(result);
            }
            catch(UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
        }

        [HttpPost("{id}/history")]
        public async Task<IActionResult> GetHistory(string id, [FromBody] GetHumanTaskInstanceHistoryQuery parameter, CancellationToken token)
        {
            try
            {
                parameter.HumanTaskInstanceId = id;
                var result = await _mediator.Send(parameter, token);
                return new OkObjectResult(result);
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
        }

        [HttpGet("{id}/description")]
        public async Task<IActionResult> GetDescription(string id, CancellationToken token)
        {
            try
            {
                var result = await _mediator.Send(new GetHumanTaskInstanceDescriptionQuery { HumanTaskInstanceId = id }, token);
                return new ContentResult
                {
                    ContentType = result.ContentType,
                    Content = result.Description,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
            catch (BadOperationExceptions ex)
            {
                var valErrors = ex.ValidationErrors.Select(_ => new KeyValuePair<string, string>("bad_request", _)).ToList();
                return this.ToError(valErrors, HttpStatusCode.BadRequest, Request);
            }
        }

        [HttpGet("{id}/subtasks")]
        public async Task<IActionResult> GetSubTasks(string id, CancellationToken token)
        {
            try
            {
                var result = await _mediator.Send(new GetHumanTaskInstanceSubTasksQuery { HumanTaskInstanceId = id }, token);
                return new OkObjectResult(result);
            }
            catch (UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
        }

        [HttpPost(".search")]
        public async Task<IActionResult> Search([FromBody] SearchHumanTaskInstanceDetailsQuery query)
        {
            query.Claims = User.GetClaims();
            var result = await _mediator.Send(query);
            return new OkObjectResult(result);
        }

        [HttpGet("{id}/rendering")]
        public async Task<IActionResult> GetRendering(string id, CancellationToken token)
        {
            try
            {
                var result = await _mediator.Send(new GetRenderingQuery { HumanTaskInstanceId = id }, token);
                return new OkObjectResult(result);
            }
            catch(UnknownHumanTaskInstanceException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.NotFound, Request);
            }
        }

        #endregion

        #region Common

        private async Task<IActionResult> SharedAdd(CreateHumanTaskInstanceCommand parameter, CancellationToken token)
        {
            try
            {
                parameter.Claims = User.GetClaims();
                var result = await _mediator.Send(parameter, token);
                return new CreatedResult(string.Empty, result);
            }
            catch (UnknownHumanTaskDefException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.BadRequest, Request);
            }
            catch (NotAuthorizedException ex)
            {
                return this.ToError(new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("bad_request", ex.Message)
                }, HttpStatusCode.Unauthorized, Request);
            }
            catch (BadOperationExceptions ex)
            {
                var valErrors = ex.ValidationErrors.Select(_ => new KeyValuePair<string, string>("bad_request", _)).ToList();
                return this.ToError(valErrors, HttpStatusCode.BadRequest, Request);
            }
        }

        #endregion
    }
}
