import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter, Subscription, interval } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private timerSub?: Subscription;
  private routeSub?: Subscription;
  private readonly nowMs = signal(Date.now());

  readonly user = this.auth.user;
  readonly remainingLabel = computed(() => {
    const expires = this.auth.sessionExpiresAt();
    if (!expires) {
      return '--:--';
    }
    const remaining = Math.max(0, Math.floor((Date.parse(expires) - this.nowMs()) / 1000));
    const minutes = Math.floor(remaining / 60);
    const seconds = remaining % 60;
    return `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
  });

  ngOnInit(): void {
    this.timerSub = interval(1000).subscribe(() => {
      this.nowMs.set(Date.now());
      const expires = this.auth.sessionExpiresAt();
      if (expires && Date.parse(expires) <= Date.now()) {
        this.auth.handleUnauthorized();
      }
    });

    this.auth.refreshSession().subscribe();
    this.routeSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => this.auth.refreshSession().subscribe());
  }

  ngOnDestroy(): void {
    this.timerSub?.unsubscribe();
    this.routeSub?.unsubscribe();
  }

  logout(): void {
    this.auth.logout().subscribe();
  }
}
