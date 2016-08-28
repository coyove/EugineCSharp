[test-closure = (lambda (n) (->
	(lambda (i) (* i n))
))]

(assert [((test-closure 4) 3) == 12] "Test closure: " 12)

(set dummy 0)
([(i) => (-> (set dummy i) (set dummy-2 i))] 1)
(assert [[dummy == 1] and [dummy-2 == null]] "Test closure 2")

;================================================

(set #test-list (1 2 3 4 5 -0.5))
(map [(i) => (++ i)] #test-list)

(assert (deep-compare-list #test-list (2 3 4 5 6 0.5)) "Map lambda")

;================================================

(set a (
	(lambda (a b c) (
		(+ (str a) "-" (str b) "-" (str c))
	)) 
	(explode (1 2 3))
))

(assert [a == "1-2-3"] "Explode")

;================================================

(defun left-padder (n c)
	"padder can either pad a string or a list"
	(lambda (subject) (->
		(cond #t
			(
				[(type subject) == "string"] 
				(for (range 0 n) [() => (set subject [c + subject])])
			)

			(
				[(type subject) == "list"] 
				(for (range 0 n) [() => [subject = (+ (list c) (explode subject))]])
				; note this implementation's performance is very poor
			)
		)
		subject
	))
)

(assert [((left-padder 5 "a") "bcd") == "aaaaabcd"] "Padding string")
(assert [((left-padder 5 "a") ("b" "c" "d")) deep-compare-list 
	("a" "a" "a" "a" "a" "b" "c" "d")] "Padding list")