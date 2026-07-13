import { DatePipe } from '@angular/common';
import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { SessionConflict } from '../../core/auth/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, DatePipe],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  email = 'admin@noticesaas.local';
  password = '';
  readonly error = signal('');
  readonly busy = signal(false);
  readonly conflict = signal<SessionConflict | null>(null);

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  submit(forceLogout = false): void {
    this.error.set('');
    this.busy.set(true);
    if (!forceLogout) {
      this.conflict.set(null);
    }

    this.auth.login(this.email.trim(), this.password, forceLogout).subscribe({
      next: (result) => {
        this.busy.set(false);
        if ('accessToken' in result) {
          void this.router.navigateByUrl('/dashboard');
          return;
        }
        this.conflict.set(result);
      },
      error: (err: Error) => {
        this.busy.set(false);
        this.error.set(err.message);
      }
    });
  }

  dismissConflict(): void {
    this.conflict.set(null);
  }
}
