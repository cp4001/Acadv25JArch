iredtee : dialog {
   label =  "Reducing Tee & Reducer";
    : column {
        :boxed_radio_row{
            label = "Appl. code ";
            : radio_button {
               label = "ANSI(API)";
               key   = "ansicode";
               value = "1";
               mnemonic = "A";
            }
            : radio_button {
               label = "KS(JIS)";
               key   = "kscode";
               value = "2";
               mnemonic = "K";
            }
        }
        : concatenation {
            : text_part {
                label = "Main Size  ";
            }
            : text_part {
                key = "zmainsize";
                width = 9;
            }
        }
        : list_box {
                    label = "Reducing Size";
                    key = "br_size";
                    width = 10;
                    allow_accept = true;
        }
    }
ok_cancel;
}

iweldolet : dialog {
   label =  "Weldolet";
    : column {
        : list_box {
                    label = "Weldolet Size";
                    key = "br_size";
                    width = 10;
                    allow_accept = true;
        }
    }
ok_cancel;
}
