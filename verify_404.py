from playwright.sync_api import sync_playwright

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()

        # 1. Test serving 404 for non-existent page
        try:
            print("Navigating to non-existent page...")
            response = page.goto("http://localhost:5000/non-existent-page")
            print(f"Status: {response.status}")

            # Verify status code is 404
            if response.status == 404:
                print("Status code verification passed: 404")
            else:
                print(f"Status code verification failed: {response.status}")

            # Wait for content
            page.wait_for_selector("text=Page not found", timeout=5000)
            print("Found text: Page not found")
            page.wait_for_selector("text=Sorry, we couldn’t find the page you’re looking for.", timeout=5000)
            print("Found description")
            page.wait_for_selector("a:has-text('Go back home')", timeout=5000)
            print("Found home link")

            page.screenshot(path="verification_404_served.png")
            print("Screenshot saved: verification_404_served.png")

        except Exception as e:
            print(f"Error testing non-existent page: {e}")

        browser.close()

if __name__ == "__main__":
    run()
