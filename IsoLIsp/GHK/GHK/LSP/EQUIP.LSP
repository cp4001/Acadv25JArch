;;;;;;10% HEAD equipment drawing lisp ( C-12 )
;;;;;;1/2 HEAD equipment drawing lisp
(defun equip(/ l d p1 p2 p3 p2a p2b p2c p2d p4 p4a p4b pt2 pt3 pt2a
             pt2b pt2c pt2d pt4 pt4a pt4b)
    (pse1)
    (command "graphscr")
    (setq l   (getreal  "\n Vessel length : "))
    (setq d   (getreal  "\n Vessel outdiameter : "))
    (setq p1  (getpoint "\n Vessel center point : "))
    (setq p2  (polar p1 (dtr 90) (/ l 2.00)))
    (cond
        ((= equi-type 1)
            (setq p3  (polar p2 (dtr 270) (* d 0.8062)));;10% C
            (setq p2a (polar p2 (dtr 0)   (* d 0.4)));;;;;10% A
            (setq p2b (polar p2 (dtr 180) (* d 0.4)));;;;;10% A
        )
        ((= equi-type 2)
            (setq p3  (polar p2 (dtr 270) (* d 0.6545)));;2:1 C
            (setq p2a (polar p2 (dtr 0)   (* d 0.3273)));;;;;2:1 A
            (setq p2b (polar p2 (dtr 180) (* d 0.3273)));;;;;2:1 A
        )
        ((= equi-type 3)
            (setq p3  (polar p2 (dtr 270) (* d 0.545)));;2:1 C
            (setq p2a (polar p2 (dtr 0)   (* d 0.345)));;;;;2:1 A
            (setq p2b (polar p2 (dtr 180) (* d 0.345)));;;;;2:1 A
        )
        ((= equi-type 4)
            (setq p3  (polar p2 (dtr 270) (* d 1.3711)));;2:1 C
            (setq p2a (polar p2 (dtr 0)   (* d 0.44)));;;;;2:1 A
            (setq p2b (polar p2 (dtr 180) (* d 0.44)));;;;;2:1 A
        )
    )
    (setq p2c (polar p2 (dtr 0)   (* d 0.5)))
    (setq p2d (polar p2 (dtr 180) (* d 0.5)))
    (cond
        ((= equi-type 1)
            (setq p4  (polar p2 (dtr 90)  (* d 0.0895)));;10% e
            (setq p4a (polar p4 (dtr 0)   (* d 0.4444)));;10% E
            (setq p4b (polar p4 (dtr 180) (* d 0.4444)));;10% E
        )
        ((= equi-type 2)
            (setq p4  (polar p2 (dtr 90)  (* d 0.1544)));;2:1 e
            (setq p4a (polar p4 (dtr 0)   (* d 0.4045)));;2:1 E
            (setq p4b (polar p4 (dtr 180) (* d 0.4045)));;2:1 E
        )
        ((= equi-type 3)
            (setq p4  (polar p2 (dtr 90)  (* d 0.1309)));;2:1 e
            (setq p4a (polar p4 (dtr 0)   (* d 0.4279)));;2:1 E
            (setq p4b (polar p4 (dtr 180) (* d 0.4279)));;2:1 E
        )
        ((= equi-type 4)
            (setq p4  (polar p2 (dtr 90)  (* d 0.0571)));;2:1 e
            (setq p4a (polar p4 (dtr 0)   (* d 0.4583)));;2:1 E
            (setq p4b (polar p4 (dtr 180) (* d 0.4583)));;2:1 E
        )
    )
    (setq pt2 (polar p1 (dtr 270) (/ l 2.00)))
    (cond
        ((= equi-type 1)
            (setq pt3  (polar pt2 (dtr 90) (* d 0.8062)));;10% C
            (setq pt2a (polar pt2 (dtr 0)   (* d 0.4)));;;;;10% A
            (setq pt2b (polar pt2 (dtr 180) (* d 0.4)));;;;;10% A
        )
        ((= equi-type 2)
            (setq pt3  (polar pt2 (dtr 90) (* d 0.6545)));;2:1 C
            (setq pt2a (polar pt2 (dtr 0)   (* d 0.3273)));;;;;2:1 A
            (setq pt2b (polar pt2 (dtr 180) (* d 0.3273)));;;;;2:1 A
        )
        ((= equi-type 3)
            (setq pt3  (polar pt2 (dtr 90) (* d 0.545)));;2:1 C
            (setq pt2a (polar pt2 (dtr 0)   (* d 0.345)));;;;;2:1 A
            (setq pt2b (polar pt2 (dtr 180) (* d 0.345)));;;;;2:1 A
        )
        ((= equi-type 4)
            (setq pt3  (polar pt2 (dtr 90) (* d 1.3711)));;2:1 C
            (setq pt2a (polar pt2 (dtr 0)   (* d 0.44)));;;;;2:1 A
            (setq pt2b (polar pt2 (dtr 180) (* d 0.44)));;;;;2:1 A
        )
    )
    (setq pt2c(polar pt2 (dtr 0)   (* d 0.5)))
    (setq pt2d(polar pt2 (dtr 180) (* d 0.5)))
    (cond
        ((= equi-type 1)
            (setq pt4  (polar pt2 (dtr 270)  (* d 0.0895)));;10% e
            (setq pt4a (polar pt4 (dtr 0)   (* d 0.4444)));;10% E
            (setq pt4b (polar pt4 (dtr 180) (* d 0.4444)));;10% E
        )
        ((= equi-type 2)
            (setq pt4  (polar pt2 (dtr 270)  (* d 0.1544)));;2:1 e
            (setq pt4a (polar pt4 (dtr 0)   (* d 0.4045)));;2:1 E
            (setq pt4b (polar pt4 (dtr 180) (* d 0.4045)));;2:1 E
        )
        ((= equi-type 3)
            (setq pt4  (polar pt2 (dtr 270)  (* d 0.1309)));;2:1 e
            (setq pt4a (polar pt4 (dtr 0)   (* d 0.4279)));;2:1 E
            (setq pt4b (polar pt4 (dtr 180) (* d 0.4279)));;2:1 E
        )
        ((= equi-type 4)
            (setq pt4  (polar pt2 (dtr 270)  (* d 0.0571)));;2:1 e
            (setq pt4a (polar pt4 (dtr 0)   (* d 0.4583)));;2:1 E
            (setq pt4b (polar pt4 (dtr 180) (* d 0.4583)));;2:1 E
        )
    )
    (setvar "osmode" 0)
    (command "layer" "s" "2C" "")
    (command "line" p2d p2c pt2c pt2d "c")
    (command "_arc" "_c" p2a p2c p4a)
    (command "_arc" "_c" p2b p4b p2d)
    (command "_arc" "_c" p3  p4a p4b)
    (command "_arc" "_c" pt2a pt4a pt2c)
    (command "_arc" "_c" pt2b pt2d pt4b)
    (command "_arc" "_c" pt3  pt4b pt4a)
    (setq equi-type nil)
    (pse2)
)
