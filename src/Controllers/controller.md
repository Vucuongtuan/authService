# Standard RESTful API Controller Structure

## Endpoints & Functions

### Get all items
- **Function name:** `index`
- **Route:** `GET /api/[items]`
- **Description:** Retrieve a list of all items  
  (supports filtering, pagination via query parameters)

---

### Get item by ID
- **Function name:** `show`
- **Route:** `GET /api/[items]/{id}`
- **Description:** Retrieve a single item by its ID (or slug)

---

### Create a new item
- **Function name:** `store` (or `create` in some conventions)
- **Route:** `POST /api/[items]`
- **Body:** Fields defined by the model
- **Description:** Create a new item

---

### Update item by ID
- **Function name:** `update`
- **Route:**  
  - `PUT /api/[items]/{id}`  
  - `PATCH /api/[items]/{id}`
- **Body:** Fields to be updated
- **Description:** Update an existing item

---

### Delete item by ID
- **Function name:** `destroy` (or `delete` / `remove`)
- **Route:** `DELETE /api/[items]/{id}`
- **Description:** Remove an item by ID

---

