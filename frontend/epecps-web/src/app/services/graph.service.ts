import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class GraphService {
  private graphApiUrl = 'https://graph.microsoft.com/v1.0';

  constructor(private http: HttpClient) {}

  /**
   * Get user's profile photo from Microsoft Graph
   * Returns a data URL that can be used as img src
   */
  getUserPhoto(): Observable<string | null> {
    return this.http.get(`${this.graphApiUrl}/me/photo/$value`, {
      responseType: 'blob'
    }).pipe(
      map(blob => {
        // Convert blob to data URL
        return URL.createObjectURL(blob);
      }),
      catchError(error => {
        console.log('No profile photo available or error fetching photo:', error);
        return of(null);
      })
    );
  }

  /**
   * Get user's profile information
   */
  getUserProfile(): Observable<any> {
    return this.http.get(`${this.graphApiUrl}/me`);
  }
}
