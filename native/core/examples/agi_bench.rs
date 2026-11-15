//! Reproducible benchmark for Phext Notepad's document core against a real
//! phext file (agi.phext by default).
//!
//! Usage: cargo run --release --example agi_bench -- <file.phext> [jump-coord]
//!
//! Measures, using only the core's public API:
//! * raw `libphext::explode` time (attribution for load)
//! * `Document::from_phext` cold load time
//! * `view()` cost at each zoom (first render after load / after a jump)
//! * coordinate jump + view latency
//! * edit cost: repeated `set_scroll` (the per-keystroke path in the app)
//! * `serialize()` full-save time, plus round-trip equality checks
//! * peak RSS (VmHWM) after load and after save

use std::time::Instant;

use phext_notepad_core::document::Document;
use phext_notepad_core::prefs::Prefs;
use phext_notepad_core::zoom::ZoomLevel;

fn vm_hwm_kb() -> u64 {
    let status = std::fs::read_to_string("/proc/self/status").unwrap_or_default();
    for line in status.lines() {
        if let Some(rest) = line.strip_prefix("VmHWM:") {
            return rest
                .trim()
                .trim_end_matches(" kB")
                .trim()
                .parse()
                .unwrap_or(0);
        }
    }
    0
}

fn median(mut values: Vec<f64>) -> f64 {
    values.sort_by(|a, b| a.partial_cmp(b).unwrap());
    values[values.len() / 2]
}

fn main() {
    let args: Vec<String> = std::env::args().collect();
    let path = args
        .get(1)
        .map(String::as_str)
        .unwrap_or("agi.phext")
        .to_string();
    let jump_target = args
        .get(2)
        .map(String::as_str)
        .unwrap_or("26.8.14/10.9.13/10.8.10")
        .to_string();

    let text = std::fs::read_to_string(&path).expect("read benchmark file");
    println!("file: {path}");
    println!("input bytes: {}", text.len());

    // --- raw libphext explode (attribution) ---------------------------------
    let mut times = Vec::new();
    for _ in 0..3 {
        let t = Instant::now();
        let map = libphext::phext::explode(&text);
        times.push(t.elapsed().as_secs_f64() * 1000.0);
        std::hint::black_box(map.len());
    }
    println!("libphext::explode: median {:.1} ms (3 runs)", median(times));

    // --- cold load -----------------------------------------------------------
    let mut times = Vec::new();
    let mut doc = Document::empty();
    for _ in 0..3 {
        let t = Instant::now();
        doc = Document::from_phext(&text, None);
        times.push(t.elapsed().as_secs_f64() * 1000.0);
    }
    println!(
        "Document::from_phext (load): median {:.1} ms (3 runs)",
        median(times)
    );
    println!("scrolls loaded: {}", doc.scrolls().len());
    println!("peak RSS after load: {} KiB", vm_hwm_kb());

    // Equivalence with the reference implementation on this real file.
    let reference: std::collections::BTreeMap<_, _> = libphext::phext::explode(&text)
        .into_iter()
        .filter(|(_, scroll)| !scroll.is_empty())
        .collect();
    println!(
        "load matches libphext::explode (empties filtered): {}",
        reference == *doc.scrolls()
    );
    let reference_bytes = {
        let map: std::collections::HashMap<_, _> = reference
            .iter()
            .map(|(coord, text)| (*coord, text.clone()))
            .collect();
        libphext::phext::implode(map)
    };
    println!(
        "serialize matches libphext::implode: {}",
        reference_bytes == doc.serialize()
    );

    let prefs = Prefs::default();

    // --- view at each zoom ----------------------------------------------------
    for zoom in [ZoomLevel::Inner, ZoomLevel::Middle, ZoomLevel::Outer] {
        doc.set_zoom(zoom);
        // warm-up + timed runs
        let _ = doc.view("bench", &prefs);
        let mut times = Vec::new();
        for _ in 0..20 {
            let t = Instant::now();
            let view = doc.view("bench", &prefs);
            times.push(t.elapsed().as_secs_f64() * 1000.0);
            std::hint::black_box(view.outline.len());
        }
        println!(
            "view() at {zoom:?}: median {:.3} ms (20 runs)",
            median(times)
        );
    }
    doc.set_zoom(ZoomLevel::Inner);

    // --- jump latency ----------------------------------------------------------
    let mut times = Vec::new();
    for _ in 0..20 {
        doc.jump("1.1.1/1.1.1/1.1.1").unwrap();
        let t = Instant::now();
        doc.jump(&jump_target).unwrap();
        let view = doc.view("bench", &prefs);
        times.push(t.elapsed().as_secs_f64() * 1000.0);
        std::hint::black_box(view.scroll.len());
    }
    println!(
        "jump to {jump_target} + view: median {:.3} ms (20 runs)",
        median(times)
    );
    println!(
        "scroll at target: {} bytes",
        doc.scroll_text().len()
    );

    // --- edit path (per-keystroke set_scroll) ----------------------------------
    // Type 200 characters into the target scroll, one set_scroll per char,
    // exactly as the app's buffer-modify handler does.
    let base = doc.scroll_text().to_string();
    let mut typed = base.clone();
    let t = Instant::now();
    for i in 0..200 {
        typed.push((b'a' + (i % 26) as u8) as char);
        doc.set_scroll(&typed);
    }
    let edit_total = t.elapsed().as_secs_f64() * 1000.0;
    println!(
        "set_scroll x200 (typing): total {:.1} ms, per call {:.3} ms",
        edit_total,
        edit_total / 200.0
    );
    doc.set_scroll(&base);

    // --- serialize (full save) --------------------------------------------------
    let mut times = Vec::new();
    let mut saved = String::new();
    for _ in 0..5 {
        let t = Instant::now();
        saved = doc.serialize();
        times.push(t.elapsed().as_secs_f64() * 1000.0);
    }
    println!(
        "serialize (full save): median {:.1} ms (5 runs)",
        median(times)
    );
    println!("serialized bytes: {}", saved.len());
    println!("peak RSS after save: {} KiB", vm_hwm_kb());

    // --- round-trip checks -------------------------------------------------------
    let reloaded = Document::from_phext(&saved, None);
    println!(
        "round-trip scroll maps equal: {}",
        reloaded.scrolls() == doc.scrolls()
    );
    println!("serialize == input bytes: {}", saved == text);
    let saved2 = reloaded.serialize();
    println!("serialize is a fixed point: {}", saved2 == saved);
}
