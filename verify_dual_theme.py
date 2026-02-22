from playwright.sync_api import sync_playwright
import time

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        context = browser.new_context()
        page = context.new_page()
        try:
            page.goto("http://localhost:8080/components/mermaid.html")

            # Wait for mermaid to render
            page.wait_for_selector(".mermaid svg", timeout=20000)

            # 1. Capture Light Mode
            print("Capturing Light Mode...")
            page.screenshot(path="mermaid_dual_light.png", full_page=True)

            # 2. Toggle Dark Mode
            print("Toggling Dark Mode...")
            page.click("#theme-toggle")
            time.sleep(1) # Wait for transition

            # 3. Capture Dark Mode
            print("Capturing Dark Mode...")
            page.screenshot(path="mermaid_dual_dark.png", full_page=True)

            # 4. Verify DOM Structure
            # Check if one mermaid element contains both light and dark containers
            mermaid_el = page.query_selector(".mermaid")
            if mermaid_el:
                light_container = mermaid_el.query_selector(".dark\\:hidden")
                dark_container = mermaid_el.query_selector(".hidden.dark\\:block")

                if light_container and dark_container:
                    print("SUCCESS: Both light and dark containers found in DOM.")
                else:
                    print("FAILURE: Missing light or dark container.")
                    print(f"Light: {light_container}, Dark: {dark_container}")
                    print(mermaid_el.inner_html())

        except Exception as e:
            print(f"Error: {e}")
        finally:
            browser.close()

if __name__ == "__main__":
    run()
