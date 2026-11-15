import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  // Default redirect
  {
    path: '',
    redirectTo: 'controls',
    pathMatch: 'full',
  },

  // Controls list
  {
    path: 'controls',
    loadComponent: () =>
      import('./features/controls/controls-list/controls-list.component').then(
        m => m.ControlsListComponent,
      ),
  },

  // Create a new control
  {
    path: 'controls/new',
    loadComponent: () =>
      import('./features/controls/control-form/control-form.component').then(
        m => m.ControlFormComponent,
      ),
  },

  // Edit an existing control
  {
    path: 'controls/:id/edit',
    loadComponent: () =>
      import('./features/controls/control-form/control-form.component').then(
        m => m.ControlFormComponent,
      ),
  },

  // Run a control
  {
    path: 'controls/:id/run',
    loadComponent: () =>
      import(
        './features/controls/control-execution/control-execution.component'
      ).then(m => m.ControlExecutionComponent),
  },

  // AI interface
  {
    path: 'ai',
    loadComponent: () =>
      import('./features/ai-interface/ai-interface.component').then(
        m => m.AiInterfaceComponent,
      ),
  },

  // Admin — metadata management (admin role required)
  {
    path: 'admin/metadata',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/metadata-admin/metadata-admin.component').then(
        m => m.MetadataAdminComponent,
      ),
  },
];
