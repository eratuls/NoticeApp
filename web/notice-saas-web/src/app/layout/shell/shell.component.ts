import { DatePipe } from '@angular/common';
import { Component, HostListener, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { filter, Subscription, interval } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { environment } from '../../../environments/environment';

interface NotificationItem {
  id: string;
  title: string;
  body: string;
  isRead: boolean;
  noticeId: string | null;
  createdAtUtc: string;
}

interface NotificationListResponse {
  unreadCount: number;
  notifications: NotificationItem[];
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, DatePipe],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit, OnDestroy {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private timerSub?: Subscription;
  private routeSub?: Subscription;
  private readonly nowMs = signal(Date.now());

  readonly user = this.auth.user;
  readonly unreadCount = signal(0);
  readonly notifications = signal<NotificationItem[]>([]);
  readonly panelOpen = signal(false);
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
    this.loadNotifications();
    this.routeSub = this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        this.auth.refreshSession().subscribe();
        this.loadNotifications();
      });
  }

  ngOnDestroy(): void {
    this.timerSub?.unsubscribe();
    this.routeSub?.unsubscribe();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.panelOpen()) {
      this.panelOpen.set(false);
    }
  }

  togglePanel(event: Event): void {
    event.stopPropagation();
    const next = !this.panelOpen();
    this.panelOpen.set(next);
    if (next) {
      this.loadNotifications();
    }
  }

  loadNotifications(): void {
    this.http
      .get<NotificationListResponse>(`${environment.apiBaseUrl}/api/v1/notifications`, {
        params: { take: 12 }
      })
      .subscribe({
        next: (res) => {
          this.notifications.set(res.notifications);
          this.unreadCount.set(res.unreadCount);
        },
        error: () => {
          /* keep prior badge state */
        }
      });
  }

  markRead(item: NotificationItem, event: Event): void {
    event.stopPropagation();
    if (item.isRead) {
      if (item.noticeId) {
        this.router.navigate(['/notices', item.noticeId]);
        this.panelOpen.set(false);
      }
      return;
    }

    this.http.post(`${environment.apiBaseUrl}/api/v1/notifications/${item.id}/read`, {}).subscribe({
      next: () => {
        this.loadNotifications();
        if (item.noticeId) {
          this.router.navigate(['/notices', item.noticeId]);
          this.panelOpen.set(false);
        }
      }
    });
  }

  markAllRead(event: Event): void {
    event.stopPropagation();
    this.http.post(`${environment.apiBaseUrl}/api/v1/notifications/read-all`, {}).subscribe({
      next: () => this.loadNotifications()
    });
  }

  logout(): void {
    this.auth.logout().subscribe();
  }
}
