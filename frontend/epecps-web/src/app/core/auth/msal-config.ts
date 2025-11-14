import {
  IPublicClientApplication,
  PublicClientApplication,
  BrowserCacheLocation,
  LogLevel,
  InteractionType
} from '@azure/msal-browser';
import {
  MsalGuardConfiguration,
  MsalInterceptorConfiguration,
  MSAL_INSTANCE,
  MSAL_GUARD_CONFIG,
  MSAL_INTERCEPTOR_CONFIG
} from '@azure/msal-angular';
import { InjectionToken } from '@angular/core';

// ========= UPDATE THESE 3 VALUES =========
// From Azure portal:
// 1) SPA app registration (client) ID
export const SPA_CLIENT_ID = 'eacd9ae9-41a7-4db4-8c53-1ca46be75081';
// 2) Tenant ID
export const TENANT_ID = '2ecda83d-4ae0-41c7-aeef-0c37c3be6826';
// 3) The API scope (full scope URI)
export const API_SCOPE = 'api://4ee96651-bdc5-40c6-a64a-49d53002ce9e/Epecps.ReadWrite';
// ========================================

// Redirect/post-logout URIs should match your dev host+port
const REDIRECT_URI = `${window.location.origin}/auth-callback`;
const POST_LOGOUT_REDIRECT_URI = `${window.location.origin}/login`;

// Microsoft Graph scopes for user profile and photo
export const GRAPH_SCOPES = ['User.Read', 'User.ReadBasic.All'];

// Optional: default login request scopes
export const DEFAULT_LOGIN_SCOPES = ['openid', 'profile', 'email', API_SCOPE, ...GRAPH_SCOPES];
export const API_SCOPES = [API_SCOPE];

export function MSALInstanceFactory(): IPublicClientApplication {
  return new PublicClientApplication({
    auth: {
      clientId: SPA_CLIENT_ID,
      authority: `https://login.microsoftonline.com/${TENANT_ID}`,
      redirectUri: REDIRECT_URI,
      postLogoutRedirectUri: POST_LOGOUT_REDIRECT_URI,
      navigateToLoginRequestUrl: true
    },
    cache: {
      cacheLocation: BrowserCacheLocation.LocalStorage,
      storeAuthStateInCookie: false
    },
    system: {
      loggerOptions: {
        logLevel: LogLevel.Warning
      }
    }
  });
}

export function MSALGuardConfigFactory(): MsalGuardConfiguration {
  return {
    interactionType: InteractionType.Redirect,
    authRequest: {
      scopes: DEFAULT_LOGIN_SCOPES
    },
    loginFailedRoute: '/login'
  };
}

export function MSALInterceptorConfigFactory(): MsalInterceptorConfiguration {
  // Any call whose URL starts with this key will get a token for the mapped scopes
  const protectedResourceMap = new Map<string, Array<string>>();
  protectedResourceMap.set('https://localhost:7275/api', API_SCOPES); // your API
  protectedResourceMap.set('https://graph.microsoft.com/v1.0', GRAPH_SCOPES); // Microsoft Graph
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap
  };
}

// Re-export tokens for app.module providers
export const MSAL_INSTANCE_TOKEN = MSAL_INSTANCE;
export const MSAL_GUARD_TOKEN = MSAL_GUARD_CONFIG;
export const MSAL_INTERCEPTOR_TOKEN = MSAL_INTERCEPTOR_CONFIG;
