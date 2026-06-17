redtee : dialog {
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
                key = "zchsize";
                width = 9;
            }
        }
        : list_box {
                    label = "Reducing Size";
                    key = "br_size";
                    width = 10;
                    allow_accept = true;
        }
        : concatenation {
            : text_part {
                label = "Brencht Dia :";
            }
            : text_part {
                key = "zbr-dia";
                width = 9;
            }
        }
        : concatenation {
            : text_part {
                label = "Brencht Length :";
            }
            : text_part {
                key = "zbr-len";
                width = 9;
            }
        }
    }
ok_cancel;
}
redsafety : dialog {
    label =  "Outlet Size" ;
        : list_box {
            label = "Out Size List";
            key   = "out_size";
            width = 3;
            allow_accept = true;
        }
  ok_cancel;
}

