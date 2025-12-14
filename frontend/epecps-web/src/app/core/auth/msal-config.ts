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

// ========= AZURE AD CONFIGURATION =========
// From Azure Portal - App Registrations

// SPA app registration (EPECPS-SPA) - Client ID
export const SPA_CLIENT_ID = 'eacd9ae9-41a7-4db4-8c53-1ca46be75081';

// Tenant ID
export const TENANT_ID = '2ecda83d-4ae0-41c7-aeef-0c37c3be6826';

// API scope (from EPECPS-API app registration)
export const API_SCOPE = 'api://4ee96651-bdc5-40c6-a64a-49d53002ce9e/Epecps.ReadWrite';
// ==========================================

// Redirect URIs - MUST match Azure AD App Registration
const REDIRECT_URI = `${window.location.origin}/auth-callback`;
const POST_LOGOUT_REDIRECT_URI = `${window.location.origin}/login`;

// Scopes
export const GRAPH_SCOPES = ['User.Read'];
export const DEFAULT_LOGIN_SCOPES = ['openid', 'profile', 'email'];
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
      storeAuthStateInCookie: true // Helps with IE/Edge issues
    },
    system: {
      loggerOptions: {
        logLevel: LogLevel.Info,
        loggerCallback: (level, message, containsPii) => {
          if (containsPii) return;
          switch (level) {
            case LogLevel.Error:
              console.error('[MSAL]', message);
              break;
            case LogLevel.Warning:
              console.warn('[MSAL]', message);
              break;
            case LogLevel.Info:
              console.info('[MSAL]', message);
              break;
            case LogLevel.Verbose:
              console.debug('[MSAL]', message);
              break;
          }
        }
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
  const protectedResourceMap = new Map<string, Array<string>>();
  
  // Protect API calls - will automatically attach access token
  protectedResourceMap.set('https://localhost:7275/api', API_SCOPES);
  
  // Protect Microsoft Graph calls
  protectedResourceMap.set('https://graph.microsoft.com/v1.0', GRAPH_SCOPES);
  
  return {
    interactionType: InteractionType.Redirect,
    protectedResourceMap
  };
}

// Re-export injection tokens
export const MSAL_INSTANCE_TOKEN = MSAL_INSTANCE;
export const MSAL_GUARD_TOKEN = MSAL_GUARD_CONFIG;
export const MSAL_INTERCEPTOR_TOKEN = MSAL_INTERCEPTOR_CONFIG;
