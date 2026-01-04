import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'game',
    pathMatch: 'full'
  },
  {
    path: 'game',
    loadComponent: () =>
      import('./features/game/game-page.component').then(m => m.GamePageComponent)
  }
];
