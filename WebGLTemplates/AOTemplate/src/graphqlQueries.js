export const fetchProcessTransactionsQuery = (address) => {
    return `{
      transactions(
        first: 100
        owners: ["${address}"]
        tags: [
          { name: "Type", values: ["Process"] }
        ]
      ) {
        edges {
          node {
            id
            tags {
              name
              value
            }
          }
        }
      }
    }`;
};
