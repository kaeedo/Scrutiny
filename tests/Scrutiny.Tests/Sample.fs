module Tests

open Expecto

[<Tests>]
let tests =
  testList "samples" [
    test "universe exists (╭ರᴥ•́)" {
      let subject = true
      Expect.isTrue subject "I compute, therefore I am."
    }
  ]
