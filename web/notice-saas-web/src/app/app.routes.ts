import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./layout/shell/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent)
      },
      {
        path: 'team',
        loadComponent: () =>
          import('./features/team/team.component').then((m) => m.TeamComponent)
      },
      {
        path: 'calendar',
        data: { title: 'Calendar' },
        loadComponent: () =>
          import('./features/placeholder/placeholder-page.component').then(
            (m) => m.PlaceholderPageComponent
          )
      },
      {
        path: 'clients',
        loadComponent: () =>
          import('./features/clients/clients.component').then((m) => m.ClientsComponent)
      },
      {
        path: 'clients/:clientId/notices',
        loadComponent: () =>
          import('./features/notices/client-notices.component').then((m) => m.ClientNoticesComponent)
      },
      {
        path: 'notices/:noticeId',
        loadComponent: () =>
          import('./features/notices/notice-detail.component').then((m) => m.NoticeDetailComponent)
      },
      {
        path: 'reminders',
        loadComponent: () =>
          import('./features/reminders/reminders.component').then((m) => m.RemindersComponent)
      },
      {
        path: 'settings/master',
        loadComponent: () =>
          import('./features/settings/master-settings.component').then(
            (m) => m.MasterSettingsComponent
          )
      },
      {
        path: 'settings/usage',
        loadComponent: () =>
          import('./features/usage/usage-limits.component').then((m) => m.UsageLimitsComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
