from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page()
    page.goto('http://localhost:8080/components/headers/hero.html')

    # Wait for content to load
    page.wait_for_selector('h1:has-text("Hero")')

    # Light Mode
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)

    # Let's just do full_page=True to see everything
    page.screenshot(path='/home/jules/hero_light_full.png', full_page=True)

    # Dark Mode
    page.evaluate("document.documentElement.classList.add('dark')")
    time.sleep(1)
    page.screenshot(path='/home/jules/hero_dark_full.png', full_page=True)

    # Do the same for CTA Panel to be safe
    page.goto('http://localhost:8080/components/headers/cta-panel.html')
    page.wait_for_selector('h1')
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)
    page.screenshot(path='/home/jules/cta_light_full.png', full_page=True)

    # Do the same for header stats
    page.goto('http://localhost:8080/components/headers/header-stats.html')
    page.wait_for_selector('h1')
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)
    page.screenshot(path='/home/jules/header_stats_light_full.png', full_page=True)

    browser.close()
