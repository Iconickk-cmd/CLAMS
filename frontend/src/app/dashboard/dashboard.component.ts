
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  inject
} from '@angular/core';

import { HttpClient } from '@angular/common/http';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { finalize } from 'rxjs/operators';

import {
  AuthService,
  CurrentUser
} from '../services/auth.service';

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
    RouterLinkActive
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {

  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly cdr = inject(ChangeDetectorRef);

  private readonly apiUrl = 'http://localhost:5219/api';

  currentUser: CurrentUser | null =
    this.authService.getCurrentUser();

  isLoading = true;
  hasLoadError = false;

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
