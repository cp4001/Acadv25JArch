valist : dialog {
    label =  "ISO Item LIst";
    : column {
        :boxed_radio_row{
            label = "Item Type";
            : radio_button {
               label = "New";
               key   = "zfuture1";
               value = "1";
            }
            : radio_button {
               label = "Exist";
               key   = "zfuture2";
               value = "2";
            }
        }
        : list_box {
            label = "Item Size";
            key = "itemlist";
            height = 15;
            allow_accept = true;
        }
    }
  ok_cancel;
}

nipple : dialog {
    label =  "Nipple End Type";
    : column {
        : concatenation {
            : text_part {
                label = "Pipe End type :";
            }
            : text_part {
                key = "zsd";
                width = 9;
            }
        }
        :boxed_radio_row{
            label = "End Type";
            : radio_button {
               label = "Plain End";
               key   = "znpe";
               value = "1";
            }
            : radio_button {
               label = "PE x TE";
               key   = "znpt";
               value = "2";
            }
            : radio_button {
               label = "Thread End";
               key   = "znte";
               value = "3";
            }
        }
        :boxed_radio_row{
            label = "Nipple Len.";
            : radio_button {
               label = "75 mm";
               key   = "z75";
               value = "1";
            }
            : radio_button {
               label = "100 mm";
               key   = "z100";
               value = "2";
            }
            : radio_button {
               label = "150 mm";
               key   = "z150";
               value = "3";
            }
        }
    }
  ok_cancel;
}

menuset : dialog {
    label =  "ISO Menu Set By g.h.kim";
    : column {
        : row {
            :boxed_radio_row{
                label = "Appl. code ";
                : radio_button {
                   label = "ANSI";
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
            :boxed_radio_row{
                label = "Size type";
                : radio_button {
                   label = "Inch";
                   key   = "zsize1";
                   value = "1";
                }
                : radio_button {
                   label = "Dia (A)";
                   key   = "zsize2";
                   value = "2";
                }
            }
        }
        : row {
            : column {
               :boxed_radio_row{
                    label = "Valve Small end";
                    : radio_button {
                       label = "Type 1";
                       key   = "zvalend1";
                       value = "1";
                    }
                    : radio_button {
                       label = "Type 2";
                       key   = "zvalend2";
                       value = "2";
                    }
                }
                : image {
                    key          = "image4";
                    width        = 15;
                    aspect_ratio = 1;
                    color        = 0;
                    allow_accept = true;
                }
            }
            : column {
                :boxed_radio_row{
                    label = "Text Style ";
                    : radio_button {
                       label = "Type 1";
                       key   = "ztext1";
                       value = "1";
                    }
                    : radio_button {
                       label = "Type 2";
                       key   = "ztext2";
                       value = "2";
                    }
                }
                : image {
                    key          = "image3";
                    width        = 15;
                    aspect_ratio = 1;
                    color        = 0;
                    allow_accept = true;
                }
            }
        }
        : row {
            : column {
                :boxed_radio_row{
                    label =  "Fitt. Conn.Type";
                    : radio_button {
                        label = "type 1";
                        key   = "zcon1";
                        value = "1";
                   }
                    : radio_button {
                        label = "type 2";
                        key   = "zcon2";
                        value = "1";
                   }
                }
                : image {
                    key          = "image1";
                    width        = 15;
                    aspect_ratio = 1;
                    color        = 0;
                    allow_accept = true;
                }
            }
            : column {
                :boxed_radio_row{
                    label =  "Hnadwhile Type";
                    : radio_button {
                        label = "type 1";
                        key   = "zhand1";
                        value = "1";
                   }
                    : radio_button {
                        label = "type 2";
                        key   = "zhand2";
                        value = "1";
                   }
                }
                : image {
                    key          = "image2";
                    width        = 15;
                    aspect_ratio = 1;
                    color        = 0;
                    allow_accept = true;
                }
            }
        }
    }
  ok_cancel;
}

lineno : dialog {
    label =  "Line no";
    : column {
        :boxed_radio_row{
            label = "Elevatio type";
            : radio_button {
               label = "BOP EL";
               key   = "zeltype1";
               value = "1";
            }
            : radio_button {
               label = "EL";
               key   = "zeltype2";
               value = "2";
            }
        }
        : concatenation {
            : text_part {
                label = "First No  :";
            }
            : edit_box  {
                key = "zfno";
                width = 10;
            }
        }
        : concatenation {
            : text_part {
                label = "Second No :";
            }
            : edit_box  {
                key = "zsno";
                width = 10;
            }
        }
        : concatenation {
            : text_part {
                label = "Class     :";
            }
            : edit_box  {
                key = "zspec";
                width = 10;
            }
        }
        : concatenation {
            : text_part {
                label = "Size      :";
            }
            : edit_box  {
                key = "zghk";
                width = 10;
            }
        }
        : concatenation {
            : text_part {
                label = "Elevation :";
            }
            : edit_box  {
                key = "zel";
                width = 10;
            }
        }
    }
  ok_cancel;
}

