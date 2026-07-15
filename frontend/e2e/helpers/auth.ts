import { expect, type Page } from '@playwright/test';

export const users = {
  admin: { email: 'admin@referwell.com', password: 'Admin@123', roleLabel: 'Admin' },
  nurse: { email: 'nurse@referwell.com', password: 'Nurse@123', roleLabel: 'Triage Nurse' },
  nurse2: { email: 'nurse2@referwell.com', password: 'Nurse2@123', roleLabel: 'Triage Nurse' },
  gp: { email: 'gp1@referwell.com', password: 'Gp1@1234', roleLabel: 'GP' },
} as const;

export async function login(page: Page, email: string, password: string) {
  await page.goto('/login');
  await page.locator('input[type="email"]').fill(email);
  await page.locator('input[type="password"]').fill(password);
  await page.getByRole('button', { name: 'Sign In' }).click();
  await expect(page).toHaveURL(/\/dashboard/, { timeout: 20_000 });
  await expect(page.getByRole('heading', { name: 'Referral Queue' })).toBeVisible();
}

export async function logout(page: Page) {
  await page.locator('nav button').filter({ hasText: /Admin|Triage Nurse|GP/ }).click();
  await page.getByRole('button', { name: 'Sign Out' }).click();
  await expect(page).toHaveURL(/\/login/);
}
