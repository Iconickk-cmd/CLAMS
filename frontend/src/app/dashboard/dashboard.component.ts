
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';

import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { forkJoin } from 'rxjs';
import { finalize } from 'rxjs/operators';

import {
  AuthService,
  CurrentUser
} from '../services/auth.service';

import {
  LeaveRequest,
  LeaveRequestService,
  LeaveRequestStatus
} from '../services/leave-request.service';

interface DashboardStatistics {
  totalEmployees: number;
  activeEmployees: number;
  departments: number;
  positions: number;
  currentAssignments: number;
  pendingLeaveRequests: number;
  approvedLeaveRequests: number;
  rejectedLeaveRequests: number;
  employeesCurrentlyOnLeave: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterLink,
    RouterLinkActive,
    DatePipe,
    DecimalPipe
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {

  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);
  private readonly leaveService = inject(LeaveRequestService);

  private readonly apiUrl = 'http://localhost:5219/api';

  currentUser: CurrentUser | null =
    this.authService.getCurrentUser();

  isLoading = true;
  hasLoadError = false;

  // Employee self-service view.
  actionTab: 'actions' | 'statements' | 'policies' = 'actions';
  summaryLoading = false;
  summaryError = false;
  leaveBalances: { leaveType: string; availableDays: number }[] = [];
  pendingRequestCount = 0;
  upcomingLeave: LeaveRequest | null = null;

  stats: DashboardStatistics = {
    totalEmployees: 0,
    activeEmployees: 0,
    departments: 0,
    positions: 0,
    currentAssignments: 0,
    pendingLeaveRequests: 0,
    approvedLeaveRequests: 0,
    rejectedLeaveRequests: 0,
    employeesCurrentlyOnLeave: 0
  };

  get isEmployeeOnly(): boolean {
    const roles = this.currentUser?.roles ?? [];

    const administrativeRoles = [
      'SystemAdministrator',
      'HRManager',
      'HROfficer',
      'Management',
      'ReadOnly'
    ];

    return roles.includes('Employee') &&
      !roles.some(role => administrativeRoles.includes(role));
  }
  get canApproveLeave(): boolean {
    const roles = this.currentUser?.roles ?? [];
    return ['SystemAdministrator', 'HRManager', 'HROfficer']
      .some(role => roles.includes(role));
  }

  ngOnInit(): void {
    if (this.isEmployeeOnly) {
      this.isLoading = false;
      this.loadEmployeeSummary();
      return;
    }

    this.loadDashboard();
  }

  loadDashboard(): void {

    this.isLoading = true;
    this.hasLoadError = false;

    this.http.get<DashboardStatistics>(
      `${this.apiUrl}/dashboard/statistics`
    ).pipe(

      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })

    ).subscribe({

      next: result => {
        this.stats = result;

        console.log(
          'CLAMS Dashboard Statistics:',
          this.stats
        );

        this.cdr.detectChanges();
      },

      error: error => {
        console.error(
          'Dashboard loading error:',
          error
        );

        this.hasLoadError = true;
        this.cdr.detectChanges();
      }

    });
  }

  loadEmployeeSummary(): void {
    this.summaryLoading = true;
    this.summaryError = false;

    forkJoin({
      balances: this.leaveService.getMyBalances(),
      requests: this.leaveService.getMyRequests()
    }).subscribe({
      next: ({ balances, requests }) => {
        // One row per leave type, summing pay categories and any
        // unexpired carry-forward.
        const totals = new Map<string, number>();
        for (const line of balances.balances ?? []) {
          totals.set(
            line.leaveType,
            (totals.get(line.leaveType) ?? 0) + line.availableDays
          );
        }
        this.leaveBalances = Array.from(totals, ([leaveType, availableDays]) => ({
          leaveType,
          availableDays
        }));

        const all = requests.requests ?? [];
        this.pendingRequestCount = all.filter(
          r => r.status === LeaveRequestStatus.Pending
        ).length;

        const now = new Date();
        const today =
          `${now.getFullYear()}-` +
          `${String(now.getMonth() + 1).padStart(2, '0')}-` +
          `${String(now.getDate()).padStart(2, '0')}`;

        this.upcomingLeave =
          all
            .filter(
              r =>
                r.status === LeaveRequestStatus.Approved &&
                r.endDate.substring(0, 10) >= today
            )
            .sort((a, b) => a.startDate.localeCompare(b.startDate))[0] ??
          null;

        this.summaryLoading = false;
        this.cdr.detectChanges();
      },
      error: error => {
        console.error('Employee summary loading error:', error);
        this.summaryError = true;
        this.summaryLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get initials(): string {
    const parts = this.userName.trim().split(/\s+/).filter(Boolean);
    const letters = parts.slice(0, 2).map(p => p.charAt(0).toUpperCase());
    return letters.join('') || 'U';
  }

  get userName(): string {
    return this.currentUser?.employeeName ?? 'Portal User';
  }

  get userRole(): string {
    return this.currentUser?.roles?.[0] ?? 'Portal User';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
