//! The xz seal — libphext 0.5.0 parity.
//!
//! Compression is a seal, not a working format. A phext is authored,
//! edited, and queried as plain text; when it is finished — archived,
//! published, or transported — it may be sealed: xz-compressed as a
//! whole (liblzma, preset 6). Unsealing restores the exact original
//! bytes, and any valid xz stream unseals, whatever preset or check
//! produced it.
//!
//! The notepad exposes the seal at this core layer only: the app has
//! no seal/open-sealed UI surface, and none was invented. The document
//! idiom is [`crate::document::Document::seal`] /
//! [`crate::document::Document::from_sealed`]; this module is the
//! string/byte-level surface, mirroring `libphext::seal`.

pub use libphext::seal::{seal, seal_bytes, unseal, unseal_string, SEAL_PRESET};

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn seal_round_trips_text_and_bytes() {
        let text = "first\x17second ✓ scroll";
        let sealed = seal(text).unwrap();
        assert_eq!(unseal(&sealed).unwrap(), text.as_bytes());
        assert_eq!(unseal_string(&sealed).unwrap(), text);
        assert_eq!(seal_bytes(text.as_bytes()).unwrap(), sealed);
        assert_eq!(SEAL_PRESET, 6);
    }

    #[test]
    fn unseal_rejects_non_xz() {
        assert!(unseal(b"plain text, not a seal").is_err());
        assert!(unseal_string(b"plain text, not a seal").is_err());
    }
}
