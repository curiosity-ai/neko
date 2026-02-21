from playwright.sync_api import sync_playwright
import os

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()

        # Determine absolute path
        cwd = os.getcwd()
        file_path = os.path.join(cwd, "TailDocs.Sample", ".taildocs", "components.html")
        url = f"file://{file_path}"

        print(f"Navigating to {url}")
        page.goto(url)

        # Wait for content to load
        page.wait_for_selector("h1")

        # Screenshot full page
        page.screenshot(path="verification/full_page.png", full_page=True)

        # Screenshot code block
        code_block = page.locator(".group").first
        if code_block.is_visible():
            code_block.screenshot(path="verification/code_block.png")
            print("Code block screenshot taken.")
        else:
            print("Code block not found.")

        # Screenshot TOC
        toc = page.locator("aside").last
        if toc.is_visible():
            toc.screenshot(path="verification/toc.png")
            print("TOC screenshot taken.")
        else:
            print("TOC not found.")

        browser.close()

if __name__ == "__main__":
    run()
