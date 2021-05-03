﻿  
import { AuthConfig } from 'angular-oauth2-oidc';

export const authConfig: AuthConfig = {
    issuer: process.env.OPENID_URL,
    clientId: 'caseManagementTasklistWebsite',
    scope: 'openid profile email role',
    redirectUri: process.env.REDIRECT_URL,
    responseType: 'code',
    requireHttps: false
}