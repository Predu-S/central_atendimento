import { test, expect } from '@playwright/test';

test('prioridade Alta tem texto branco no tooltip e na legenda no tema escuro', async ({ page }) => {
  await page.addInitScript(() => localStorage.setItem('central-theme', 'light'));
  await page.route('**/api/**', async route => {
    const path = new URL(route.request().url()).pathname;
    const data = path.endsWith('/series') ? {
      porDia: [], porCanal: [], porTecnico: [],
      porPrioridade: [{ nome: 'Critica', quantidade: 3 }, { nome: 'Alta', quantidade: 7 }],
    } : path.endsWith('/resumo') || path.endsWith('/regras') ? null : [];
    await route.fulfill({ json: data });
  });
  await page.goto('/');
  const panel = page.locator('.panel').filter({ has: page.getByRole('heading', { name: 'Distribuição por prioridade' }) });
  const sector = panel.locator('.recharts-sector[name="Alta"]');
  const tooltip = panel.locator('.recharts-tooltip-item');
  const legend = panel.locator('.recharts-legend-item-text').filter({ hasText: 'Alta' });
  await expect(sector).toBeVisible();
  await page.waitForTimeout(1600); // Recharts animates and replaces sectors on mount.
  // Find a filled point, since the ring's bounding box includes its empty center.
  const point = await sector.evaluate(element => {
    const path = element as SVGPathElement;
    const box = path.getBBox();
    for (let x = 0.1; x < 1; x += 0.1) for (let y = 0.1; y < 1; y += 0.1) {
      const point = new DOMPoint(box.x + box.width * x, box.y + box.height * y);
      if (path.isPointInFill(point)) {
        const screen = point.matrixTransform(path.getScreenCTM()!);
        return { x: screen.x, y: screen.y };
      }
    }
    throw new Error('Setor Alta não encontrado');
  });
  await page.mouse.move(point.x, point.y);
  await expect(tooltip).toBeVisible();
  await expect(tooltip).toContainText('Alta');
  const lightColor = await tooltip.evaluate(el => getComputedStyle(el).color);
  await page.getByRole('button', { name: 'Ativar tema escuro' }).click();
  await page.mouse.move(point.x, point.y);
  await expect(tooltip).toHaveCSS('color', 'rgb(255, 255, 255)');
  await expect(tooltip.locator('.recharts-tooltip-item-name')).toHaveCSS('color', 'rgb(255, 255, 255)');
  await expect(legend).toHaveCSS('color', 'rgb(255, 255, 255)');
  await expect(sector).toHaveAttribute('fill', '#f59e0b');
  await page.getByRole('button', { name: 'Ativar tema claro' }).click();
  await page.mouse.move(point.x, point.y);
  await expect(tooltip).toHaveCSS('color', lightColor);
});
