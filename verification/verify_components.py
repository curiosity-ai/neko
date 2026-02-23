from playwright.sync_api import sync_playwright
import time

def verify(page, component):
    url = f"http://localhost:8000/components/headers/{component}.html"
    print(f"Verifying {component} at {url}")
    try:
        page.goto(url)
        # Wait for potential images to load (cat images)
        page.wait_for_timeout(2000)
        path = f"verification/{component}.png"
        page.screenshot(path=path, full_page=True)
        print(f"Screenshot saved to {path}")
    except Exception as e:
        print(f"Failed to verify {component}: {e}")

if __name__ == "__main__":
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        page = browser.new_page()

        # Verify specific components
        verify(page, "bento-grid")
        verify(page, "content-sticky")
        verify(page, "feature-grid")
        verify(page, "pricing-tiers")

        browser.close()
