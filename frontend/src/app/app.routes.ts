import { Routes } from '@angular/router';

import { LoginComponent } from './login/login.component';
import { ActivateAccountComponent } from './activate-account/activate-account.component';
import { DashboardComponent } from './dashboard/dashboard.component';
import { EmployeeComponent } from './employees/employee.component';
import { DepartmentComponent } from './departments/department.component';
import { PositionComponent } from './positions/position.component';
import { PositionAssignmentComponent } from './position-assignments/position-assignment.component';
import { LeaveTypeComponent } from './leave-types/leave-type.component';
import { LeaveEntitlementPolicyComponent } from './leave-entitlement-policies/leave-entitlement-policy.component';
import { LeaveRequestComponent } from './leave-requests/leave-request.component';
import { LeaveApprovalComponent } from './leave-approvals/leave-approval.component';
import { MyProfileComponent } from './profile/my-profile.component';
import { AccessDeniedComponent } from './access-denied/access-denied.component';

import { roleGuard } from './guards/role.guard';

export const routes: Routes = [

  {
    path: 'login',
    component: LoginComponent
  },
  {
    path: 'activate-account',
    component: ActivateAccountComponent
  },


  {
    path: 'access-denied',
    component: AccessDeniedComponent
  },

  {
    path: '',
    component: DashboardComponent,
    canActivate: [roleGuard],
    data: {
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly', 'Employee']
    }
  },

  {
    path: 'dashboard',
    component: DashboardComponent,
    canActivate: [roleGuard],
    data: {
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly', 'Employee']
    }
  },

  {
    path: 'employees',
    component: EmployeeComponent,
    canActivate: [roleGuard],
    data: {
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer']
    }
  },

  {
    path: 'departments',
    component: DepartmentComponent,
    canActivate: [roleGuard],
    data: {
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly']
    }
  },

  {
    path: 'positions',
    component: PositionComponent,
    canActivate: [roleGuard],
    data: {
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly']
    }
  },

  {
    path: 'position-assignments',
    component: PositionAssignmentComponent,
    canActivate: [roleGuard],
    data: {
      title: 'Position Assignments',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly']
    }
  },

  {
    path: 'leave-types',
    component: LeaveTypeComponent,
    canActivate: [roleGuard],
    data: {
      title: 'Leave Types Management',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly']
    }
  },

  {
    path: 'leave-entitlement-policies',
    component: LeaveEntitlementPolicyComponent,
    canActivate: [roleGuard],
    data: {
      title: 'Leave Entitlement Policies',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly']
    }
  },

  {
    path: 'leave-requests',
    component: LeaveRequestComponent,
    canActivate: [roleGuard],
    data: {
      title: 'My Leave Applications',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Employee']
    }
  },

  {
    path: 'leave-approvals',
    component: LeaveApprovalComponent,
    canActivate: [roleGuard],
    data: {
      title: 'Leave Approvals',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer']
    }
  },
  {
    path: 'profile',
    component: MyProfileComponent,
    canActivate: [roleGuard],
    data: {
      title: 'My Profile',
      section: 'profile',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly', 'Employee']
    }
  },
  {
    path: 'next-of-kin',
    component: MyProfileComponent,
    canActivate: [roleGuard],
    data: {
      title: 'Next of Kin',
      section: 'next-of-kin',
      roles: ['SystemAdministrator', 'HRManager', 'HROfficer', 'Management', 'ReadOnly', 'Employee']
    }
  },
  {
    path: '**',
    redirectTo: ''
  }

];


