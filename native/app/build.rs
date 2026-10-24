fn main() {
    // FLTK is C++. rustc asks the linker for libsupc++, which lives in GCC's
    // private directory. `g++ -print-file-name` is the portable way to find it
    // on Ubuntu 22.04 and newer, where the directory includes the GCC version.
    if !cfg!(target_os = "linux") {
        return;
    }
    let Ok(output) = std::process::Command::new("g++")
        .args(["-print-file-name=libsupc++.a"])
        .output()
    else {
        return;
    };
    let Ok(path) = String::from_utf8(output.stdout) else {
        return;
    };
    let path = path.trim();
    if path.is_empty() || path == "libsupc++.a" {
        return;
    }
    if let Some(dir) = std::path::Path::new(path).parent() {
        println!("cargo:rustc-link-search=native={}", dir.display());
    }
}
