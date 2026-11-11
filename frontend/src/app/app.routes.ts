import { Routes } from '@angular/router';

import { LoginComponent } from './login/login.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { EmployeeComponent } from './employees/employee.component';
import { DepartmentComponent } from './departments/department.component';
import { PositionComponent } from './positions/position.component';
import { PositionAssignmentComponent } from './position-assignments/position-assignment.component';
import { LeaveTypeComponent } from './leave-types/leave-type.component';
import { LeaveEntitlementPolicyComponent } from './leave-entitlement-policies/leave-entitlement-policy.component';

import { ModulePlaceholderComponent } from './shared/module-placeholder.component';
import { authGuard } from './guards/auth.guard';

export const routes: Routes = [

  {
    path: 'login',
    component: LoginComponent
  },

  {
    path: '',
    component: DashboardComponent,
    canActivate: [authGuard]
  },

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [authGuard]
  },

  {
    path: 'employees',
    component: EmployeeComponent,
    canActivate: [authGuard]
  },

  {
    path: 'departments',
    component: DepartmentComponent,
    canActivate: [authGuard]
  },

  {
    path: 'positions',
    component: PositionComponent,
    canActivate: [authGuard]
  },

  {
    path: 'position-assignments',
    component: PositionAssignmentComponent,
    canActivate: [authGuard],
    data: {
      title: 'Position Assignments'
    }
  },

  {
    path: 'leave-types',
    component: LeaveTypeComponent,
    canActivate: [authGuard],
    data: {
      title: 'Leave Types Management'
    }
  },

  {
    path: 'leave-entitlement-policies',
    component: LeaveEntitlementPolicyComponent,
    canActivate: [authGuard],
    data: {
      title: 'Leave Entitlement Policies'
    }
  },

  {
    path: '**',
    redirectTo: ''
  }

];
