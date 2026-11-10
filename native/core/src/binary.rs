//! Binary scrolls and byte-level ingestion — libphext 0.5.0 parity.
//!
//! A binary scroll stores arbitrary bytes as text: the header line
//! `phext-binary:1:base64`, a line break, then the RFC 4648 base64 of
//! the payload (standard alphabet, padding required, no wrapping). An
//! empty payload is the header line alone. A scroll beginning with the
//! header line is a binary scroll by definition; one whose body is not
//! strict base64 is malformed and fetches verbatim.
//!
//! The ingestion rule: when non-UTF-8 data is observed in an incoming
//! scroll update or byte stream, it is encoded with base64 verbatim —
//! never rejected, never normalised — so the matching byte fetch
//! returns the original bytes exactly.
//!
//! These are the reference implementation's own functions, re-exported
//! so the notepad's byte surface is the canonical one by construction.
//! The document-level idiom (current-coordinate byte access) lives in
//! [`crate::document::Document`]; this module is the string-level
//! surface, mirroring `libphext::binary`.

pub use libphext::binary::{
    decode_scroll, encode_scroll, fetch_binary, ingest, insert_binary, insert_bytes,
    is_binary_scroll, phokenize_bytes, replace_binary, replace_bytes, BINARY_SCROLL_HEADER,
};

#[cfg(test)]
mod tests {
    use super::*;
    use libphext::phext;

    fn coord(address: &str) -> phext::Coordinate {
        phext::to_coordinate(address)
    }

    /// Ported 1:1 in substance from libphext's `test_binary.rs`: the
    /// notepad core must agree with the reference on every leg.
    #[test]
    fn encode_decode_round_trip() {
        let empty: Vec<u8> = vec![];
        let scroll = encode_scroll(&empty);
        assert_eq!(scroll, "phext-binary:1:base64\n");
        assert_eq!(decode_scroll(&scroll), Some(empty));

        let all_bytes: Vec<u8> = (0..=255u8).collect();
        let scroll = encode_scroll(&all_bytes);
        assert!(is_binary_scroll(&scroll));
        assert_eq!(decode_scroll(&scroll), Some(all_bytes));

        let big: Vec<u8> = (0..(1024 * 1024 + 13)).map(|i| (i % 256) as u8).collect();
        let scroll = encode_scroll(&big);
        assert_eq!(decode_scroll(&scroll), Some(big));
    }

    #[test]
    fn decode_rejects_non_binary_and_malformed() {
        assert_eq!(decode_scroll("just some text"), None);
        assert_eq!(decode_scroll(""), None);
        assert_eq!(decode_scroll("phext-binary:1:base64"), None);
        assert!(!is_binary_scroll("phext-binary:1:base64"));
        let malformed = "phext-binary:1:base64\n!!! not base64 !!!";
        assert!(is_binary_scroll(malformed));
        assert_eq!(decode_scroll(malformed), None);
    }

    #[test]
    fn insert_and_fetch_binary_at_unbounded_coordinates() {
        // Past every historical cap: the 0.3.1 ceiling (1000), the old
        // WinForms wrap (32,767) — a 1 MiB payload's home in the parity
        // harness as well.
        let target = coord("1.1.1/1.1.1/1.1.1006");
        let payload: Vec<u8> = vec![0x00, 0x01, 0x02, 0xFE, 0xFF, 0x7F, 0x80];
        let doc = phext::insert(String::new(), coord("1.1.1/1.1.1/1.1.1"), "plain text scroll");
        let doc = insert_binary(doc, target, &payload);
        assert_eq!(phext::fetch(&doc, coord("1.1.1/1.1.1/1.1.1")), "plain text scroll");
        assert_eq!(fetch_binary(&doc, target), payload);
        let stored = phext::fetch(&doc, target);
        assert!(is_binary_scroll(&stored));
        assert_eq!(decode_scroll(&stored), Some(payload));
    }

    #[test]
    fn fetch_binary_fallbacks() {
        let doc = phext::insert(String::new(), coord("1.1.1/1.1.1/1.1.1"), "hello");
        assert_eq!(fetch_binary(&doc, coord("1.1.1/1.1.1/1.1.1")), b"hello".to_vec());
        assert_eq!(fetch_binary(&doc, coord("9.9.9/9.9.9/9.9.9")), Vec::<u8>::new());
        let doc = phext::insert(
            String::new(),
            coord("1.1.1/1.1.1/1.1.1"),
            "phext-binary:1:base64\n!!!",
        );
        assert_eq!(
            fetch_binary(&doc, coord("1.1.1/1.1.1/1.1.1")),
            b"phext-binary:1:base64\n!!!".to_vec()
        );
    }

    #[test]
    fn update_rule_non_utf8_is_base64_verbatim() {
        let target = coord("1.1.1/1.1.1/1.1.1");
        let payload: Vec<u8> = vec![0xFF, 0x17, 0x1C, 0x80, 0x00];
        let doc = replace_bytes("", target, &payload);
        let stored = phext::fetch(&doc, target);
        assert!(is_binary_scroll(&stored));
        assert_eq!(fetch_binary(&doc, target), payload);
        let doc = insert_bytes(String::new(), target, &payload);
        assert_eq!(fetch_binary(&doc, target), payload);
    }

    #[test]
    fn update_rule_utf8_stays_text() {
        let target = coord("1.1.1/1.1.1/1.1.1");
        let doc = replace_bytes("", target, "ordinary text ✓".as_bytes());
        assert_eq!(phext::fetch(&doc, target), "ordinary text ✓");
        assert!(!is_binary_scroll(&phext::fetch(&doc, target)));
    }

    #[test]
    fn ingest_foreign_and_canonical_streams() {
        let mut stream: Vec<u8> = Vec::new();
        stream.extend_from_slice(b"hello");
        stream.push(phext::SCROLL_BREAK_U8);
        stream.extend_from_slice(&[0xFF, 0xFE, 0x41, 0x80]);
        let doc = ingest(&stream);
        assert_eq!(phext::fetch(&doc, coord("1.1.1/1.1.1/1.1.1")), "hello");
        let foreign_at = coord("1.1.1/1.1.1/1.1.2");
        assert!(is_binary_scroll(&phext::fetch(&doc, foreign_at)));
        assert_eq!(fetch_binary(&doc, foreign_at), vec![0xFF, 0xFE, 0x41, 0x80]);

        let mut doc = phext::insert(String::new(), coord("1.1.1/1.1.1/1.1.1"), "first");
        doc = phext::insert(doc, coord("2.1.1/1.3.1/1.1.1"), "far away");
        assert_eq!(ingest(doc.as_bytes()), doc);
        assert_eq!(ingest(&[]), "");
    }

    #[test]
    fn phokenize_bytes_matches_phokenize() {
        let doc = "alpha\x17beta\x18gamma";
        let text_parts = phext::phokenize(doc);
        let byte_parts = phokenize_bytes(doc.as_bytes());
        assert_eq!(text_parts.len(), byte_parts.len());
        for (text_part, byte_part) in text_parts.iter().zip(byte_parts.iter()) {
            assert_eq!(text_part.coord, byte_part.0);
            assert_eq!(text_part.scroll.as_bytes(), byte_part.1.as_slice());
        }
    }
}
