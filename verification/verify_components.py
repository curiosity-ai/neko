from playwright.sync_api import sync_playwright
import os

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()

        # Determine absolute path
        cwd = os.getcwd()
        # Updated to point to TailDocs.Documentation
        # Note: This assumes the site has been built to .taildocs inside the documentation folder
        # or that you are serving it.
        # But let's just update the path reference.
        file_path = os.path.join(cwd, "TailDocs.Documentation", ".taildocs", "index.html")
        url = f"file://{file_path}"

        print(f"Navigating to {url}")
        try:
            page.goto(url)

            # Wait for content to load
            page.wait_for_selector("h1")

            # Screenshot full page
            page.screenshot(path="verification/full_page.png", full_page=True)

            print("Full page screenshot taken.")

        except Exception as e:
            print(f"Error: {e}")

        browser.close()

if __name__ == "__main__":
    run()
