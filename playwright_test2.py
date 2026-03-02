from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page()

    # HERO
    page.goto('http://localhost:8080/components/headers/hero.html')
    page.wait_for_selector('h1')
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)
    page.locator('.not-prose').nth(0).screenshot(path='/home/jules/hero_el_light.png')

    page.evaluate("document.documentElement.classList.add('dark')")
    time.sleep(1)
    page.locator('.not-prose').nth(0).screenshot(path='/home/jules/hero_el_dark.png')

    # CTA
    page.goto('http://localhost:8080/components/headers/cta-panel.html')
    page.wait_for_selector('h1')
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)
    page.locator('.not-prose').nth(0).screenshot(path='/home/jules/cta_el_light.png')

    # STATS
    page.goto('http://localhost:8080/components/headers/header-stats.html')
    page.wait_for_selector('h1')
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)
    page.locator('.not-prose').nth(0).screenshot(path='/home/jules/stats_el_light.png')

    browser.close()
