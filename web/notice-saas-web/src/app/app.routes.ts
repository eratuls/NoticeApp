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
        data: { title: 'Team' },
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
        path: 'reminders',
        data: { title: 'Reminders' },
        loadComponent: () =>
          import('./features/placeholder/placeholder-page.component').then(
            (m) => m.PlaceholderPageComponent
          )
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
