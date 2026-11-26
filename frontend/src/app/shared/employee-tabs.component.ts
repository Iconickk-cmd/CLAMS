import { Component, Input } from '@angular/core';
import { RouterLink } from '@angular/router';

export type EmployeeTab = 'overview' | 'profile' | 'next-of-kin' | 'leave';

/** Tab strip shared by the employee dashboard, profile and leave pages. */
@Component({
  selector: 'app-employee-tabs',
  standalone: true,
  imports: [RouterLink],
  template: `
    <nav class="tabs" aria-label="Employee sections">
      <a routerLink="/dashboard" class="tab" [class.active]="active === 'overview'">Overview</a>
      <a routerLink="/profile" class="tab" [class.active]="active === 'profile'">Profile</a>
      <a routerLink="/next-of-kin" class="tab" [class.active]="active === 'next-of-kin'">Next of Kin</a>
      <a routerLink="/leave-requests" class="tab" [class.active]="active === 'leave'">Leave</a>
      <span class="tab disabled" title="Coming soon">Documents</span>
    </nav>
  `,
  styles: [`
    :host {
      display: block;
    }

    .tabs {
      display: flex;
      gap: 4px;
      padding: 0 30px;
      overflow-x: auto;
      background: #fff;
      border-bottom: 1px solid #e1e5ed;
    }

    .tab {
      padding: 18px 22px 16px;
      border-bottom: 3px solid transparent;
      color: #5b6577;
      font-size: 16px;
      text-decoration: none;
      white-space: nowrap;
    }

    .tab.active {
      border-bottom-color: #315bce;
      color: #17264f;
      font-weight: 600;
    }

    .tab.disabled {
      color: #aab2bf;
      cursor: default;
    }

    .tab:hover:not(.active):not(.disabled) {
      color: #315bce;
    }

    @media (max-width: 600px) {
      .tabs {
        padding: 0 14px;
      }
    }
  `]
})
export class EmployeeTabsComponent {
  @Input() active: EmployeeTab = 'overview';
}
