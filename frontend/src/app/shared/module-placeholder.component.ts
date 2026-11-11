import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-module-placeholder',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="placeholder-page">

      <div class="placeholder-card">

        <div class="placeholder-icon">CLAMS</div>

        <h1>{{ title }}</h1>

        <p>
          This module is being prepared for the CLAMS Human Resource
          Management System.
        </p>

        <a routerLink="/dashboard">
          Return to Dashboard
        </a>

      </div>

    </div>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
    }

    .placeholder-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #f4f7f5;
      padding: 20px;
      font-family: Arial, sans-serif;
    }

    .placeholder-card {
      width: 450px;
      max-width: 100%;
      padding: 45px 30px;
      text-align: center;
      background: #ffffff;
      border-radius: 12px;
      box-shadow: 0 8px 30px rgba(30, 70, 40, 0.08);
    }

    .placeholder-icon {
      color: #176b37;
      font-size: 24px;
      font-weight: 800;
      margin-bottom: 20px;
    }

    h1 {
      color: #293b2e;
      font-size: 22px;
    }

    p {
      color: #879188;
      font-size: 13px;
      line-height: 1.7;
      margin: 15px 0 25px;
    }

    a {
      display: inline-block;
      background: #24743e;
      color: #ffffff;
      text-decoration: none;
      padding: 11px 20px;
      border-radius: 6px;
      font-size: 13px;
    }
  `]
})
export class ModulePlaceholderComponent {

  private readonly route = inject(ActivatedRoute);

  title = String(
    this.route.snapshot.data['title'] ?? 'HR Module'
  );
}
