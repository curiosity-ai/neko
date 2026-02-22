from playwright.sync_api import sync_playwright
import os

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Load the local HTML file
        file_path = os.path.abspath("dist/index.html")
        page.goto(f"file://{file_path}")

        # Wait for sidebar to be visible (it is rendered server-side but let's be safe)
        sidebar = page.locator("#sidebar")

        # Take a screenshot of the sidebar
        sidebar.screenshot(path="verification/sidebar.png")

        browser.close()

if __name__ == "__main__":
    run()
