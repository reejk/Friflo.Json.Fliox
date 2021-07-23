import { Article, DerivedClass, Order } from "../PocStore/UnitTest.Flow.Graph"


var exampleArticle: Article = {
    id: "article-id",
    name: "Article Name",
    producer: "producer-id"
}

var exampleOrder: Order = {
    id: "order-id",
    customer: "customer-id",
    created: "2021-07-22T06:00:00.000Z",
    items: [
        {
            amount: 1, 
            article: "article-id",
            name: "Article Name",
        }
    ]
}

var exampleDerivedClass: DerivedClass = {
    amount:     1, // ensure access to derived property
    derivedVal: 2
}

export function testPocStore() {
}