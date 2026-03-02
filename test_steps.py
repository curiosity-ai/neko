from playwright.sync_api import sync_playwright
import time

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page()
    page.goto('http://localhost:8080/components/steps.html')

    # Wait for content to load
    page.wait_for_selector('h1:has-text("Steps")')

    # Light Mode
    page.evaluate("document.documentElement.classList.remove('dark')")
    time.sleep(1)
    page.screenshot(path='/home/jules/steps_light_full.png', full_page=True)

    # Dark Mode
    page.evaluate("document.documentElement.classList.add('dark')")
    time.sleep(1)
    page.screenshot(path='/home/jules/steps_dark_full.png', full_page=True)
    browser.close()
