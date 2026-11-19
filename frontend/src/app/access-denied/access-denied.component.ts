import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="access-denied-page">
      <div class="access-denied-card">
        <h1>Access Denied</h1>
        <p>
          You do not have permission to access this section
          of the CLAMS system.
        </p>
        <a routerLink="/login">Return to Login</a>
      </div>
    </main>
  `,
  styles: [`
    .access-denied-page {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px;
      background: #111318;
      font-family: Arial, sans-serif;
    }

    .access-denied-card {
      width: 100%;
      max-width: 440px;
      padding: 40px;
      border-radius: 12px;
      background: #ffffff;
      text-align: center;
    }

    h1 {
      color: #17264f;
      margin-bottom: 16px;
    }

    p {
      color: #555555;
      line-height: 1.6;
    }

    a {
      display: inline-block;
      margin-top: 20px;
      padding: 12px 22px;
      border-radius: 6px;
      background: #315bce;
      color: #ffffff;
      text-decoration: none;
    }
  `]
})
export class AccessDeniedComponent {}
