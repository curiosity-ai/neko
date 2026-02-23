from playwright.sync_api import sync_playwright

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()

        try:
            print("Navigating to index.html...")
            # Capture console messages
            page.on("console", lambda msg: print(f"Console: {msg.text}"))
            page.on("pageerror", lambda exc: print(f"PageError: {exc}"))

            response = page.goto("http://localhost:5000/index")
            print(f"Status: {response.status}")
            print(f"Title: {page.title()}")

            # Check if theme toggle works
            page.click("#theme-toggle")
            print("Clicked theme toggle")

            page.screenshot(path="verification_index.png")
            print("Screenshot saved: verification_index.png")

        except Exception as e:
            print(f"Error testing index.html: {e}")

        browser.close()

if __name__ == "__main__":
    run()
