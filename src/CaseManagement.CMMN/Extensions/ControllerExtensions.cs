﻿using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

namespace CaseManagement.CMMN.Extensions
{
    public static class ControllerExtensions
    {
        public static string GetNameIdentifier(this Controller controller)
        {
            var name = (controller.User.Identity as ClaimsIdentity).Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value;
            return name;
        }
    }
}