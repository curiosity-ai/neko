from playwright.sync_api import sync_playwright

def run(playwright):
    browser = playwright.chromium.launch(headless=True)
    page = browser.new_page()
    page.goto("https://retype.com/components/code-block/")

    # Wait for content
    page.wait_for_selector("code")

    # Take screenshot of a code block example
    # Usually retype has examples on the page.
    page.screenshot(path="verification/retype_code_block.png", full_page=True)

    # Also capture TOC
    # TOC is usually on the right side.
    # Let's take another screenshot of the layout.

    browser.close()

with sync_playwright() as playwright:
    run(playwright)
