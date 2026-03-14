import { test, expect } from '@playwright/test';

function getHeader(headers: Record<string, string>, name: string): string | undefined {
  const lower = name.toLowerCase();
  const entry = Object.entries(headers).find(([k]) => k.toLowerCase() === lower);
  return entry?.[1];
}

test.describe('Security headers', () => {
  test('homepage returns required security headers', async ({ page }) => {
    const response = await page.goto('/');
    expect(response).toBeTruthy();
    expect(response!.status()).toBe(200);

    const headers = response!.headers();
    const xFrameOptions = getHeader(headers, 'x-frame-options');
    const contentSecurityPolicy = getHeader(headers, 'content-security-policy');
    const xContentTypeOptions = getHeader(headers, 'x-content-type-options');

    expect(
      xFrameOptions || contentSecurityPolicy,
      'x-frame-options or content-security-policy must be present'
    ).toBeTruthy();
    expect(xContentTypeOptions, 'x-content-type-options must be present').toBeTruthy();
    expect(
      xContentTypeOptions!.toLowerCase(),
      'x-content-type-options must be nosniff'
    ).toBe('nosniff');
  });
});
