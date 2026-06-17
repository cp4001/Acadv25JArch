ssspec : dialog {
   label =  "Spec List By g.h.kim";
   : list_box {
               label = "Class List";
               key = "z_list";
               width = 8;
               allow_accept = true;
   }
  ok_cancel;
}

ftof : dialog {
   label =  "PIPE FIT. TO FIT. CALCU.";
: row {
   : list_box {
               label = "PIPE";
               key = "mainsize";
               width = 6;
               allow_accept = true;
   }
: boxed_column {
     :paragraph {
      : text_part {
              label =  "----PIPE FIT. TO FIT. CALCU.----";
      }
      : concatenation {
          : text_part {
              label = "PIPE SIZE          :";
          }
          : text_part {
              key = "zpipe-size";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "90Elbow Length     :";
          }
          : text_part {
              key = "zell90";
              width = 5;
          }
          :edit_box  {
              key = "zell90q";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "90Sort Elbow Length:";
          }
          : text_part {
              key = "zell90s";
              width = 5;
          }
          :edit_box  {
              key = "zell90sq";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "45Elbow Length     :";
          }
          : text_part {
              key = "zell45";
              width = 5;
          }
          :edit_box  {
              key = "zell45q";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "TEE Length         :";
          }
          : text_part {
              key = "zlentee";
              width = 5;
          }
          :edit_box  {
              key = "zlenteeq";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "RED Length         :";
          }
          : text_part {
              key = "zlenred";
              width = 5;
          }
          :edit_box  {
              key = "zlenredq";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "PIPE Length        :";
          }
          :edit_box  {
              key = "zfitlen";
              width = 5;
          }
      }
     }
}
}

  ok_cancel;
}

ssbolt : dialog {
   label =  "BOLT/NUT  CALCULATION";
: boxed_column {
     :paragraph {
      : text_part {
         label =      "     -----BOLT/NUT------  ";
      }
      : concatenation {
          : text_part {
              label = "PIPE SIZE         :";
          }
          : text_part {
              key = "zghk";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "BOLT SIZE         :";
          }
          : text_part {
              key = "zsize";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "BOLT LENGTH       :";
          }
          : text_part {
              key = "zlen";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "BOLT Q'TY         :";
          }
          : edit_box  {
              key = "zqty";
              width = 5;
          }
      }
      : concatenation {
          : text_part {
              label = "ADD BOLT LENGTH   :";
          }
          : edit_box  {
              key = "zlenadd";
              width = 5;
          }
      }
     }
}

  ok_cancel;
}

specchange : dialog {
   label =  "Spec Change";
    : row {
        : list_box {
               label = "Old Spec";
               key = "o_list";
               width = 8;
               allow_accept = true;
        }
        : list_box {
               label = "New Spec";
               key = "n_list";
               width = 8;
               allow_accept = true;
        }
    }
  ok_cancel;
}

