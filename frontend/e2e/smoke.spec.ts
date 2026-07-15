import { test, expect } from '@playwright/test';
import { login, logout, users } from './helpers/auth';

test.describe('Auth', () => {
  test('login page renders brand and form', async ({ page }) => {
    await page.goto('/login');
    await expect(page.getByText('Refer').first()).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Sign in to your account' })).toBeVisible();
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible();
  });

  test('rejects invalid credentials', async ({ page }) => {
    await page.goto('/login');
    await page.locator('input[type="email"]').fill('nobody@referwell.com');
    await page.locator('input[type="password"]').fill('WrongPass1!');
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page.locator('.text-red-600')).toBeVisible({ timeout: 15_000 });
    await expect(page).toHaveURL(/\/login/);
  });

  test('admin can sign in and sign out', async ({ page }) => {
    await login(page, users.admin.email, users.admin.password);
    await expect(page.getByText(users.admin.roleLabel).first()).toBeVisible();
    await logout(page);
    await expect(page.getByRole('heading', { name: 'Sign in to your account' })).toBeVisible();
  });
});

test.describe('Role landing pages', () => {
  test('admin lands on referral queue with admin nav', async ({ page }) => {
    await login(page, users.admin.email, users.admin.password);
    await expect(page.getByRole('heading', { name: 'Referral Queue' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'User Management' })).toBeVisible();
    await expect(page.getByText('Total Referrals')).toBeVisible();
  });

  test('triage nurse lands on referral queue', async ({ page }) => {
    await login(page, users.nurse.email, users.nurse.password);
    await expect(page.getByRole('heading', { name: 'Referral Queue' })).toBeVisible();
    await expect(page.getByText(/Triage Nurse/i).first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Refresh' })).toBeVisible();
  });

  test('GP lands on queue and can open new referral', async ({ page }) => {
    await login(page, users.gp.email, users.gp.password);
    await expect(page.getByRole('heading', { name: 'Referral Queue' })).toBeVisible();
    await expect(page.getByText(/Referrals you submitted/i)).toBeVisible();

    await page.getByRole('button', { name: 'New Referral' }).click();
    await expect(page).toHaveURL(/\/referrals\/new/);
    await expect(page.getByRole('heading', { name: 'New Specialist Referral' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Select Patient' })).toBeVisible();
  });
});

test.describe('Admin navigation', () => {
  test.beforeEach(async ({ page }) => {
    await login(page, users.admin.email, users.admin.password);
  });

  test('opens user management', async ({ page }) => {
    await page.getByRole('link', { name: 'User Management' }).click();
    await expect(page).toHaveURL(/\/users/);
    await expect(page.getByRole('heading', { name: 'User Management' })).toBeVisible();
  });

  test('opens priority config', async ({ page }) => {
    await page.getByRole('link', { name: 'Priority Config' }).click();
    await expect(page).toHaveURL(/\/configs/);
    await expect(page.getByRole('heading', { name: 'Priority Formula Configuration' })).toBeVisible();
  });

  test('opens mass communications', async ({ page }) => {
    await page.getByRole('link', { name: 'Mass Communications' }).click();
    await expect(page).toHaveURL(/\/mass-comm/);
    await expect(page.getByRole('heading', { name: 'Referral status communications' })).toBeVisible();
  });
});

test.describe('Referral queue smoke', () => {
  test('nurse can refresh queue and see filter controls', async ({ page }) => {
    await login(page, users.nurse.email, users.nurse.password);
    await page.getByRole('button', { name: 'Refresh' }).click();
    await expect(page.getByRole('heading', { name: 'Referral Queue' })).toBeVisible();
    await expect(page.getByPlaceholder(/Search patient by Name or NHI/i)).toBeVisible();
    await expect(page.getByPlaceholder(/Search by Case Number/i)).toBeVisible();
  });
});
