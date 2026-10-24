//! Write the baseline document at each zoom so the UI can be opened in a browser.
//!
//! Usage: cargo run -p phext-notepad-core --bin dump-fixture > native/ui/fixture.json

use phext_notepad_core::{Document, Prefs, ZoomLevel};

fn main() {
    let prefs = Prefs::default();
    let mut doc = Document::baseline();
    doc.set_zoom(ZoomLevel::Inner);
    let inner = doc.view("open", &prefs);
    doc.set_zoom(ZoomLevel::Middle);
    let middle = doc.view("navigate", &prefs);
    doc.set_zoom(ZoomLevel::Outer);
    let outer = doc.view("navigate", &prefs);
    let scrolls: Vec<(String, String)> = doc
        .scrolls()
        .iter()
        .map(|(coord, text)| (format!("{coord}"), text.clone()))
        .collect();
    let payload = serde_json::json!({
        "inner": inner,
        "middle": middle,
        "outer": outer,
        "scrolls": scrolls,
    });
    println!("{}", serde_json::to_string_pretty(&payload).unwrap());
}
