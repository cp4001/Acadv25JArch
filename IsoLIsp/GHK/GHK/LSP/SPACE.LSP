(defun spacemain(/ dcl_id sizelist thklist ratlist
              lst1 mlst1 mlst1- mlst10 mlst10- m-dia bw a-pip k-pip
              slst1 slst1- slst10 slst10- s-dia sdist_len sdist_last add_len)
 (pse1)
 (setq dcl_id (load_dialog "c:/ghk/lsp/space.dcl"))
 (if (not (new_dialog "space" dcl_id))
     (exit)
 )
 (setq sizelist
    (list "1/2   (15A)" "3/4   (20A)" "1     (25A)" "1 1/2 (40A)"
          "2     (50A)" "2 1/2 (65A)" "3     (80A)" "4    (100A)"
          "5    (125A)" "6    (150A)" "8    (200A)" "10   (250A)"
          "12   (300A)" "14   (350A)" "16   (400A)" "18   (450A)"
          "20   (500A)" "22   (550A)" "24   (600A)" "26   (650A)"
          "28   (700A)" "30   (750A)" "32   (800A)" "34   (850A)"
          "36   (900A)" "38   (950A)" "40  (1000A)" "42  (1050A)"
          "44  (1100A)" "46  (1150A)" "48  (1200A)" "50  (1250A)"
          "52  (1300A)" "54  (1350A)" "56  (1400A)" "58  (1450A)" "60  (1500A)"
    )
 )
 (setq thklist
    (list "0" "25" "30" "40" "50" "60" "65" "70" "75" "80" "90" "100" "110"
          "120" "130" "140" "150" "160" "170" "180" "190" "200" "210" "220"
          "230" "240" "250" "260" "270" "275"
    )
 )
 (setq ratlist (list "150#" "300#" "600#" "900#" "1500#" "2500#"))
 (if (not mainthk)(setq mainthk "0"))
 (if (not subthk)(setq subthk "0"))
 (if (not mainrat)(setq mainrat "150#"))
 (if (not subrat)(setq subrat "150#"))
 (if (not sdist)(setq sdist "0"))
 (if (not sdist1)(setq sdist1 "0"))
 (if (not num1)(setq num1 "0"))
 (if (not num2)(setq num2 "0"))
 (if (not num3)(setq num3 "0"))
 (if (not num4)(setq num4 "0"))
 (if (not mainnum)(setq mainnum "0"))
 (if (not subnum)(setq subnum "0"))
 (if (= m-type 0)
    (progn
        (mode_tile "mrat" 0)
        (set_tile "zmflg" "1")
        (set_tile "zmpip" "0")
        (p-data "flg")
        (setq mlst1 lst1)
        (setq mlst1- (nth (atoi mainnum) mlst1));size select
        (setq mlst10(nth 0 mlst1-));flg
        (spa-set2)
    )
    (progn
        (set_tile "zmpip" "1")
        (set_tile "zmflg" "0")
        (mode_tile "mrat" 1)
        (p-data "pipe")
        (setq mlst1 lst1)
        (setq mlst1- (nth (atoi mainnum) mlst1));size select
        (setq bw (nth 0 mlst1-))
        (setq a-pip(nth 10 bw) k-pip(nth 11 bw))
        (if (= 1 ks-)(setq m-dia k-pip)(setq m-dia a-pip))
    )
 )
 (if (= s-type 0)
    (progn
        (mode_tile "srat" 0)
        (set_tile "zsflg" "1")
        (set_tile "zspip" "0")
        (p-data "flg")
        (setq slst1 lst1)
        (setq slst1- (nth (atoi subnum) slst1));size select
        (setq slst10(nth 0 slst1-));flg
        (spa-set4)
    )
    (progn
        (set_tile "zspip" "1")
        (set_tile "zsflg" "0")
        (mode_tile "srat" 1)
        (p-data "pipe")
        (setq slst1 lst1)
        (setq slst1- (nth (atoi subnum) slst1));size select
        (setq bw (nth 0 slst1-))
        (setq a-pip(nth 10 bw) k-pip(nth 11 bw))
        (if (= 1 ks-)(setq s-dia k-pip)(setq s-dia a-pip))
    )
 )
 (action_tile "mainp"  "(mainp_1 (setq mainnum $value))")
 (action_tile "subp"   "(mainp_2 (setq subnum  $value))")
 (action_tile "mthk"   "(mthk (setq num1 $value))")
 (action_tile "sthk"   "(sthk (setq num2 $value))")
 (action_tile "mrat"   "(mrat (setq num3 $value))")
 (action_tile "srat"   "(srat (setq num4 $value))")
 (action_tile "zmflg"   "(setq m-type 0)(spa-set1)")
 (action_tile "zmpip"   "(setq m-type 1)(spa-set1)")
 (action_tile "zsflg"   "(setq s-type 0)(spa-set3)")
 (action_tile "zspip"   "(setq s-type 1)(spa-set3)")
 (action_tile "zm-dia" "(setq m-dia $value)")
 (action_tile "zs-dia" "(setq s-dia $value)")
 (action_tile "zsdist" "(setq sdist $value)")
 (action_tile "zsdist1" "(setq sdist1 $value)")
 (start_list "mthk")
 (mapcar 'add_list thklist)
 (end_list)
 (start_list "sthk")
 (mapcar 'add_list thklist)
 (end_list)
 (start_list "mrat")
 (mapcar 'add_list ratlist)
 (end_list)
 (start_list "srat")
 (mapcar 'add_list ratlist)
 (end_list)
 (start_list "mainp")
 (mapcar 'add_list sizelist)
 (end_list)
 (start_list "subp")
 (mapcar 'add_list sizelist)
 (end_list)
 (set_tile "mthk" num1)
 (set_tile "sthk" num2)
 (set_tile "mrat" num3)
 (set_tile "srat" num4)
 (set_tile "mainp" mainnum)
 (set_tile "subp"  subnum)
 (set_tile "zm-dia" m-dia)
 (set_tile "zs-dia" s-dia)
 (set_tile "zsdist" sdist)
 (set_tile "zsdist1" sdist1)
 (setq ok(start_dialog))
 (done_dialog)
 (unload_dialog dcl_id)
 (if (= ok 1)
    (progn
        (initget 1)
        (command "offset" sdist pause pause "")
    )
 )
 (pse2)
)

(defun mainp_1(data)
 (if (= m-type 0)
    (progn
        (set_tile "zmflg" "1")
        (set_tile "zmpip" "0")
        (mode_tile "mrat" 0)
        (setq mlst1- (nth (atoi data) mlst1));size select
        (setq mlst10(nth 0 mlst1-));flg
        (spa-set2)
    )
    (progn
        (set_tile "zmpip" "1")
        (set_tile "zmflg" "0")
        (mode_tile "mrat" 1)
        (setq mlst1- (nth (atoi data) mlst1))
        (setq bw (nth 0 mlst1-))
        (setq a-pip(nth 10 bw) k-pip(nth 11 bw))
        (if (= 1 ks-)(setq m-dia k-pip)(setq m-dia a-pip))
    )
 )
 (start_list "subp")
 (mapcar 'add_list sizelist)
 (end_list)
 (set_tile "zm-dia" m-dia)
 (set_tile "zsdist" sdist)
)

(defun mainp_2(data)
 (if (= s-type 0)
    (progn
        (set_tile "zsflg" "1")
        (set_tile "zspip" "0")
        (mode_tile "srat" 0)
        (setq slst1- (nth (atoi data) slst1));size select
        (setq slst10(nth 0 slst1-));flg
        (spa-set4)
    )
    (progn
        (set_tile "zspip" "1")
        (set_tile "zsflg" "0")
        (mode_tile "srat" 1)
        (setq slst1- (nth (atoi data) slst1))
        (setq bw (nth 0 slst1-))
        (setq a-pip(nth 10 bw) k-pip(nth 11 bw))
        (if (= 1 ks-)(setq s-dia k-pip)(setq s-dia a-pip))
    )
 )
 (set_tile "zs-dia" s-dia)
 (sdistace)
)

(defun mthk(data)
 (setq mainthk (nth (atoi data) thklist))
 (sdistace)
)
(defun sthk(data)
 (setq subthk (nth (atoi data) thklist))
 (sdistace)
)

(defun mrat(data)
 (setq mainrat (nth (atoi data) ratlist))
 (spa-set2)
 (set_tile "zm-dia" m-dia)
 (sdistace)
)

(defun srat(data)
 (setq subrat (nth (atoi data) ratlist))
 (spa-set4)
 (set_tile "zs-dia" s-dia)
 (sdistace)
)

(defun sdistace ()
 (setq sdist(+ (/ (+ (atof m-dia) (atof s-dia)) 2.00) 25 (atof mainthk) (atof subthk)))
 (setq sdist1(rtos (+ (/ (+ (atof m-dia) (atof s-dia)) 2.00) 25 (atof mainthk) (atof subthk)) 2 0))
 (if (> 160 sdist)
    (setq sdist "160")
    (progn
        (setq sdist(rtos sdist 2 0))
        (setq sdist_len(strlen sdist))
        (setq sdist_last(atof (substr sdist sdist_len)))
        (setq add_len(- 10 sdist_last))
        (if (/= 0 add_len)
            (setq sdist(rtos (+ (atof sdist) add_len) 2 0))
        )
    )
 )
 (set_tile "zsdist1" sdist1)
 (set_tile "zsdist" sdist)
)
(defun spa-set1()
 (if (= m-type 0)
    (progn
        (set_tile "zmflg" "1")
        (set_tile "zmpip" "0")
        (mode_tile "mrat" 0)
        (p-data "flg")
        (setq mlst1 lst1)
        (setq mlst1- (nth (atoi mainnum) mlst1));size select
        (setq mlst10(nth 0 mlst1-));flg
        (spa-set2)
    )
    (progn
        (set_tile "zmpip" "1")
        (set_tile "zmflg" "0")
        (mode_tile "mrat" 1)
        (p-data "pipe")
        (setq mlst1 lst1)
        (setq mlst1- (nth (atoi mainnum) mlst1));size select
        (setq bw (nth 0 mlst1-))
        (setq a-pip(nth 10 bw) k-pip(nth 11 bw))
        (if (= 1 ks-)(setq m-dia k-pip)(setq m-dia a-pip))
    )
 )
)
(defun spa-set2()
 (cond
     ((= mainrat "150#")
         (setq mlst10-(nth 0 mlst10));150#
     )
     ((= mainrat "300#")
         (setq mlst10-(nth 1 mlst10));300#
     )
     ((= mainrat "600#")
         (setq mlst10-(nth 2 mlst10));600#
     )
     ((= mainrat "900#")
         (setq mlst10-(nth 3 mlst10));900#
     )
     ((= mainrat "1500#")
         (setq mlst10-(nth 4 mlst10));1500#
     )
     ((= mainrat "2500#")
         (setq mlst10-(nth 5 mlst10));2500#
     )
 )
 (setq m-dia(nth 2 mlst10-))
)
(defun spa-set3()
 (if (= s-type 0)
    (progn
        (set_tile "zsflg" "1")
        (set_tile "zspip" "0")
        (mode_tile "srat" 0)
        (p-data "flg")
        (setq slst1 lst1)
        (setq slst1- (nth (atoi subnum) slst1));size select
        (setq slst10(nth 0 slst1-));flg
        (spa-set4)
    )
    (progn
        (set_tile "zspip" "1")
        (set_tile "zsflg" "0")
        (mode_tile "srat" 1)
        (p-data "pipe")
        (setq slst1 lst1)
        (setq slst1- (nth (atoi subnum) slst1));size select
        (setq bw (nth 0 slst1-))
        (setq a-pip(nth 10 bw) k-pip(nth 11 bw))
        (if (= 1 ks-)(setq s-dia k-pip)(setq s-dia a-pip))
    )
 )
)
(defun spa-set4()
 (cond
     ((= subrat "150#")
         (setq slst10-(nth 0 slst10));150#
     )
     ((= subrat "300#")
         (setq slst10-(nth 1 slst10));300#
     )
     ((= subrat "600#")
         (setq slst10-(nth 2 slst10));600#
     )
     ((= subrat "900#")
         (setq slst10-(nth 3 slst10));900#
     )
     ((= subrat "1500#")
         (setq slst10-(nth 4 slst10));1500#
     )
     ((= subrat "2500#")
         (setq slst10-(nth 5 slst10));2500#
     )
 )
 (setq s-dia(nth 2 slst10-))
)
(defun space(/ q_con)
    (IF (not space)(LOAD"space"))(spacemain)
    (setq q_con (strcase (getstring "\nDo you want to continue < Yes / No > : ")))
    (if (= q_con "N")(setq ggg nil)(setq ggg 7))
    (while ggg
        (spacemain)
        (setq q_con (strcase (getstring "\nDo you want to continue < Yes / No > : ")))
        (if (= q_con "N")(setq ggg nil)(setq ggg 7))
    )
)
