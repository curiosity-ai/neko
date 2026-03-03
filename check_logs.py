from playwright.sync_api import sync_playwright

with sync_playwright() as p:
    browser = p.chromium.launch()
    page = browser.new_page()
    page.on("console", lambda msg: print(f"Console {msg.type}: {msg.text}"))
    page.goto("http://localhost:8080/components/cards.html")
    page.wait_for_timeout(2000)
    browser.close()
