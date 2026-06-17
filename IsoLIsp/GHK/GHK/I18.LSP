(defun c:tet(/ sum i sss l en el text1 text snu d1 b1)
    (setvar "cmdecho" 0)
    (setq sum 0)
    (setq i 0)
    (setq sss(ssget))
    (setq l(sslength sss))
    (repeat l
        (setq en(ssname sss i))
        (setq el (entget en))
        (setq text1 (assoc 1 el))
        (setq text (cdr (assoc 1 el)))
        (setq snu (strcase text))
        (setq d1(cons (car text1) snu))
        (setq b1 (subst d1 text1 el))
        (entmod b1)
        (setq i(+ 1 i))
    )
)

(defun C:I56 ()
   (defun *error* (s)
       (princ s)   
       (setq *error* nil)
       (terpri)
   )
   (setq blk (car (entsel "\nSELECT OBJECT TO EXPLODE\n")))
   (if (not (equal (cdr (assoc 0 (entget blk))) "INSERT"))
       (command "explode" blk)
       (progn 
           (setq blkname (cdr (assoc 2 (entget blk)))
                   xprop (cdr (assoc 41 (entget blk)))
                   yprop (cdr (assoc 42 (entget blk)))
                   zprop (cdr (assoc 43 (entget blk)))
                   inspt (cdr (assoc 10 (entget blk)))
                  rotang (atof (angtos (cdr (assoc 50 (entget blk)))
                            (getvar "aunits") (getvar "auprec")))
           )
           (if (null xprop) (setq xprop 1))
           (if (null yprop) (setq yprop 1))
           (if (null zprop) (setq zprop 1))
           (if (and (equal xprop yprop) (equal yprop zprop)
                    (equal xprop zprop))
               (command "explode" blk)
               (if (and (not (equal (substr blkname 1 1) "*"))
                             (equal (abs xprop) (abs yprop)))
                   (progn
                      (princ "\nTHIS INSERTION IS A MIRRORED BLOCK! 
                                         REDEFINING IT")
                      (setvar "cmdecho" 0)
                      (setq last (entlast))
                      (command "erase" blk ""
                         "insert" (strcat "*" blkname) inspt (abs xprop) "0")
                      (setq insgrp (ssadd))
                      (while last
                             (setq last (entnext last))
                             (if last (ssadd last insgrp))
                      )
                      (if (minusp xprop)
                          (command "mirror" insgrp "" inspt "@0,12" "y")
                      )
                      (if (minusp yprop)
                          (command "mirror" insgrp "" inspt "@12,0" "Y")
                      )
                      (setvar "rotate" "p" "" inspt rotang) 
                      (setvar "cmdecho" 1)
                   );;progn
               );;if
           );;if
       );;progn
   );;if
   (princ "\nDONE!")
   (print)
);;defun


