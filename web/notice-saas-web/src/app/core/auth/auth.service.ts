import { Injectable, computed, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, catchError, map, of, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  LoginSuccess,
  LoginUser,
  SessionConflict,
  SessionStatus,
  clearAuth,
  readSessionExpiresAt,
  readToken,
  readUser,
  storeAuth,
  writeSessionExpiresAt
} from './auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly userSignal = signal<LoginUser | null>(readUser());
  private readonly sessionExpiresSignal = signal<string | null>(readSessionExpiresAt());

  readonly user = this.userSignal.asReadonly();
  readonly sessionExpiresAt = this.sessionExpiresSignal.asReadonly();
  readonly isAuthenticated = computed(() => !!readToken() && !!this.userSignal());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  login(email: string, password: string, forceLogout = false): Observable<LoginSuccess | SessionConflict> {
    return this.http
      .post<LoginSuccess>(`${environment.apiBaseUrl}/api/auth/login`, {
        email,
        password,
        forceLogout
      })
      .pipe(
        tap((success) => this.applySuccess(success)),
        catchError((err: HttpErrorResponse) => {
          if (err.status === 409 && err.error?.code === 'SESSION_ACTIVE') {
            return of(err.error as SessionConflict);
          }
          if (err.status === 0) {
            return throwError(
              () =>
                new Error(
                  'Cannot reach API at http://localhost:5166. Start NoticeSaaS.Api with the http profile (not IIS Express).'
                )
            );
          }
          const message =
            typeof err.error?.message === 'string' ? err.error.message : 'Invalid email or password.';
          return throwError(() => new Error(message));
        })
      );
  }

  logout(): Observable<void> {
    const request$ = readToken()
      ? this.http.post<void>(`${environment.apiBaseUrl}/api/auth/logout`, {}).pipe(
          catchError(() => of(void 0))
        )
      : of(void 0);

    return request$.pipe(
      tap(() => {
        this.clearLocal();
        void this.router.navigateByUrl('/login');
      }),
      map(() => void 0)
    );
  }

  refreshSession(): Observable<SessionStatus | null> {
    if (!readToken()) {
      return of(null);
    }

    return this.http.get<SessionStatus>(`${environment.apiBaseUrl}/api/auth/session`).pipe(
      tap((status) => {
        this.userSignal.set(status.user);
        this.sessionExpiresSignal.set(status.expiresAtUtc);
        writeSessionExpiresAt(status.expiresAtUtc);
      }),
      catchError(() => {
        this.clearLocal();
        return of(null);
      })
    );
  }

  handleUnauthorized(): void {
    this.clearLocal();
    void this.router.navigateByUrl('/login');
  }

  private applySuccess(success: LoginSuccess): void {
    storeAuth(success);
    this.userSignal.set(success.user);
    this.sessionExpiresSignal.set(success.sessionExpiresAtUtc);
  }

  private clearLocal(): void {
    clearAuth();
    this.userSignal.set(null);
    this.sessionExpiresSignal.set(null);
  }
}
