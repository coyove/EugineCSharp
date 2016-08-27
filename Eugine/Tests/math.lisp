(defun calc-pi (n) (->
	(set p16 1)
	(set pi 0)

    (for (range 0 n)
		(lambda (i) (->
			(defun pp (a b) [a / [[8 * i] + b]])
			; +--------------------+
			; |            a       |
			; | pp = ------------- |
			; |        i * 8 + b   |
			; +--------------------+

			[pi += [[1.0 / p16] * (- (pp 4 1) (pp 2 4) (pp 1 5) (pp 1 6))]]
			; +-----------------------------------------------------------+
			; |         1                                                 |
			; | pi += ----- * (pp(4, 1) - pp(2, 4) - pp(1, 5) - pp(1, 6)) |
			; |        p16                                                |
			; +-----------------------------------------------------------+

    		[p16 *= 16]
		))
	)

	pi
))

()

(assert [(sub [#pi = (str (calc-pi 10))] 0 10) == "3.14159265"] "PI: " #pi)
(set #pi-2 [[16 * (atan [1 / 5])] - [4 * (atan [1 / 239])]])
(assert [(sub #pi 0 10) == (sub (str #pi-2) 0 10)] "PI2 check: " #pi-2)

(defun calc-e (nsteps) (->
	[res = 2.0]
	[fact = 1]

	(for (range 2 nsteps) [(i) => (
		[fact *= i]
		[res += [1 / fact]]
	)])

	(res)
))

(assert [(sub [#e = (str (calc-e 20))] 0 10) == "2.71828182"] "E: " #e)

(assert [(and #t (== #t null)) == #f] "True and False")
(assert [(or #t #f #t) == #t] "True or False or True")
(assert [(not #t) == #f] "Not True")

(defun is-prime (n) 
    (if [[n == 2] || [n == 3]] #t
    	(if [[n % 2] == 0] #f
    		(->
    			[i = 3]
    			[ret = #t]
    			(for [[i * i] <= n] [() => 					; begin loop
					(if [[n % i] == 0] 
						(-> [ret = #f] #f) 	; exit loop
						(-> [i += 2] #t)	; next loop
					)
				])

			    ret
			)
		)
	)
)

(set #prime-list (list))
(loop (range 1 100) [(i) => (-> (if (is-prime i) [#prime-list += i]) #t)])
(assert (deep-compare-list 
	#prime-list 
	(1 2 3 5 7 11 13 17 19 23 29 31 37 41 43 47 53 59 61 67 71 73 79 83 89 97))
	"Prime list < 100"
)
